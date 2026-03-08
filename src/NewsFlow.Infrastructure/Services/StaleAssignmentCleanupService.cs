using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Domain.Enums;

namespace NewsFlow.Infrastructure.Services;

public class StaleAssignmentCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StaleAssignmentCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15);
    private readonly TimeSpan _staleThreshold = TimeSpan.FromHours(4);

    public StaleAssignmentCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<StaleAssignmentCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StaleAssignmentCleanupService запущен (порог: {Threshold}, интервал: {Interval})",
            _staleThreshold, _checkInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupStaleAssignmentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при очистке зависших назначений");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CleanupStaleAssignmentsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var cutoff = DateTime.UtcNow - _staleThreshold;

        var releasedMaterials = await ReleaseStaleMaterialsAsync(db, cutoff, notifications, ct);
        var releasedDocuments = await ReleaseStaleDocumentsAsync(db, cutoff, notifications, ct);

        if (releasedMaterials + releasedDocuments > 0)
        {
            _logger.LogInformation("Очищено зависших назначений: материалов {Materials}, документов {Documents}",
                releasedMaterials, releasedDocuments);
        }
    }

    private static async Task<int> ReleaseStaleMaterialsAsync(
        IApplicationDbContext db, DateTime cutoff, INotificationService notifications, CancellationToken ct)
    {
        var staleMaterials = await db.Materials
            .Where(m => m.AssignedToId != null
                && m.AssignedAt != null
                && m.AssignedAt < cutoff
                && (m.Status == MaterialStatus.InTranslation || m.Status == MaterialStatus.InAnalysis))
            .ToListAsync(ct);

        foreach (var m in staleMaterials)
        {
            var targetRole = m.Status switch
            {
                MaterialStatus.InTranslation => "Translator",
                MaterialStatus.InAnalysis => "Analyst",
                _ => null
            };

            if (m.Status == MaterialStatus.InTranslation)
                m.ReleaseFromTranslation();
            else if (m.Status == MaterialStatus.InAnalysis)
                m.ReleaseFromAnalysis();

            if (targetRole is not null)
                await notifications.NotifyRoleAsync(targetRole,
                    $"Материал возвращён в очередь (таймаут): {m.Title}", "Material", m.Id);
        }

        if (staleMaterials.Count > 0)
            await db.SaveChangesAsync(ct);

        return staleMaterials.Count;
    }

    private static async Task<int> ReleaseStaleDocumentsAsync(
        IApplicationDbContext db, DateTime cutoff, INotificationService notifications, CancellationToken ct)
    {
        var staleDocuments = await db.Documents
            .Where(d => d.AssignedToId != null
                && d.AssignedAt != null
                && d.AssignedAt < cutoff
                && (d.Status == DocumentStatus.InReview
                    || d.Status == DocumentStatus.InRegistration
                    || d.Status == DocumentStatus.InControl
                    || d.Status == DocumentStatus.InEvaluation))
            .ToListAsync(ct);

        foreach (var d in staleDocuments)
        {
            var targetRole = d.Status switch
            {
                DocumentStatus.InReview => "Reviewer",
                DocumentStatus.InRegistration => "Registrar",
                DocumentStatus.InControl => "Controller",
                DocumentStatus.InEvaluation => "Evaluator",
                _ => null
            };

            d.ReleaseStaleAssignment();

            if (targetRole is not null)
                await notifications.NotifyRoleAsync(targetRole,
                    $"Документ возвращён в очередь (таймаут): {d.Title}", "Document", d.Id);
        }

        if (staleDocuments.Count > 0)
            await db.SaveChangesAsync(ct);

        return staleDocuments.Count;
    }
}
