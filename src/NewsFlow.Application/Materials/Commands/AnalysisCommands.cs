using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.Materials.DTOs;
using NewsFlow.Domain.Enums;

namespace NewsFlow.Application.Materials.Commands;

// === Take for analysis ===
public record TakeMaterialForAnalysisCommand(Guid MaterialId, int? RowVersion = null) : IRequest<MaterialDto>;

public class TakeForAnalysisHandler : IRequestHandler<TakeMaterialForAnalysisCommand, MaterialDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public TakeForAnalysisHandler(IApplicationDbContext db, ICurrentUserService cu)
    {
        _db = db;
        _currentUser = cu;
    }

    public async Task<MaterialDto> Handle(TakeMaterialForAnalysisCommand r, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        var m = await _db.Materials
            .Include(m => m.OriginalLanguage).Include(m => m.Source).Include(m => m.Country)
            .Include(m => m.CreatedBy).Include(m => m.AssignedTo).Include(m => m.Attachments).Include(m => m.Tags)
            .FirstOrDefaultAsync(m => m.Id == r.MaterialId, ct)
            ?? throw new KeyNotFoundException("Material not found");
        if (r.RowVersion.HasValue) _db.SetOriginalRowVersion(m, r.RowVersion.Value);
        m.TakeForAnalysis(userId);
        await _db.SaveChangesAsync(ct);
        return CreateMaterialHandler.ToDto(m);
    }
}

// === Complete analysis ===
public record CompleteMaterialAnalysisCommand(Guid MaterialId, int? RowVersion = null) : IRequest;

public class CompleteAnalysisHandler : IRequestHandler<CompleteMaterialAnalysisCommand>
{
    private readonly IApplicationDbContext _db;

    public CompleteAnalysisHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(CompleteMaterialAnalysisCommand r, CancellationToken ct)
    {
        var m = await _db.Materials.FindAsync([r.MaterialId], ct) ?? throw new KeyNotFoundException();
        if (r.RowVersion.HasValue) _db.SetOriginalRowVersion(m, r.RowVersion.Value);
        m.CompleteAnalysis();
        await _db.SaveChangesAsync(ct);
    }
}

// === Release from analysis ===
public record ReleaseMaterialFromAnalysisCommand(Guid MaterialId, int? RowVersion = null) : IRequest;

public class ReleaseFromAnalysisHandler : IRequestHandler<ReleaseMaterialFromAnalysisCommand>
{
    private readonly IApplicationDbContext _db;

    public ReleaseFromAnalysisHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(ReleaseMaterialFromAnalysisCommand r, CancellationToken ct)
    {
        var m = await _db.Materials.FindAsync([r.MaterialId], ct) ?? throw new KeyNotFoundException();
        if (r.RowVersion.HasValue) _db.SetOriginalRowVersion(m, r.RowVersion.Value);
        m.ReleaseFromAnalysis();
        await _db.SaveChangesAsync(ct);
    }
}

// === Return to translation ===
public record ReturnMaterialToTranslationCommand(Guid MaterialId, string? Reason, int? RowVersion = null) : IRequest;

public class ReturnToTranslationHandler : IRequestHandler<ReturnMaterialToTranslationCommand>
{
    private readonly IApplicationDbContext _db;

    public ReturnToTranslationHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(ReturnMaterialToTranslationCommand r, CancellationToken ct)
    {
        var m = await _db.Materials.FindAsync([r.MaterialId], ct) ?? throw new KeyNotFoundException();
        if (r.RowVersion.HasValue) _db.SetOriginalRowVersion(m, r.RowVersion.Value);
        m.ReturnToTranslation(r.Reason);
        await _db.SaveChangesAsync(ct);
    }
}

// === Mark as not of interest ===
public record MarkMaterialNotOfInterestCommand(Guid MaterialId, string? Reason, int? RowVersion = null) : IRequest;

public class MarkNotOfInterestHandler : IRequestHandler<MarkMaterialNotOfInterestCommand>
{
    private readonly IApplicationDbContext _db;

    public MarkNotOfInterestHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(MarkMaterialNotOfInterestCommand r, CancellationToken ct)
    {
        var m = await _db.Materials.FindAsync([r.MaterialId], ct) ?? throw new KeyNotFoundException();
        if (r.RowVersion.HasValue) _db.SetOriginalRowVersion(m, r.RowVersion.Value);
        m.MarkAsNotOfInterest(r.Reason);
        await _db.SaveChangesAsync(ct);
    }
}

// === Mark as distorted ===
public record MarkMaterialDistortedCommand(Guid MaterialId, string? Reason, int? RowVersion = null) : IRequest;

public class MarkDistortedHandler : IRequestHandler<MarkMaterialDistortedCommand>
{
    private readonly IApplicationDbContext _db;

    public MarkDistortedHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(MarkMaterialDistortedCommand r, CancellationToken ct)
    {
        var m = await _db.Materials.FindAsync([r.MaterialId], ct) ?? throw new KeyNotFoundException();
        if (r.RowVersion.HasValue) _db.SetOriginalRowVersion(m, r.RowVersion.Value);
        m.MarkAsDistorted(r.Reason);
        await _db.SaveChangesAsync(ct);
    }
}

// === Analyst queue ===
public record GetAnalystQueueQuery(int Page = 1, int PageSize = 50)
    : IRequest<PagedResult<MaterialListItemDto>>;

public class GetAnalystQueueHandler : IRequestHandler<GetAnalystQueueQuery, PagedResult<MaterialListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetAnalystQueueHandler(IApplicationDbContext db, ICurrentUserService cu)
    {
        _db = db;
        _currentUser = cu;
    }

    public async Task<PagedResult<MaterialListItemDto>> Handle(GetAnalystQueueQuery r, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var q = _db.Materials
            .Include(m => m.OriginalLanguage).Include(m => m.Source).Include(m => m.Country).Include(m => m.AssignedTo)
            .Where(m =>
                // Свободные переведённые материалы в очереди
                (m.Status == MaterialStatus.Translated && m.AssignedToId == null)
                // Материалы, взятые текущим пользователем в анализ
                || (m.Status == MaterialStatus.InAnalysis && m.AssignedToId == userId)
            );

        var total = await q.CountAsync(ct);
        var items = await q
            // Сначала "мои" в работе, потом свободные
            .OrderByDescending(m => m.Status == MaterialStatus.InAnalysis && m.AssignedToId == userId ? 1 : 0)
            .ThenByDescending(m => m.Priority)
            .ThenBy(m => m.ReceivedAt)
            .Skip((r.Page - 1) * r.PageSize).Take(r.PageSize)
            .Select(m => new MaterialListItemDto(
                m.Id, m.Title, m.OriginalLanguage.Name, m.Source.Name,
                m.Country != null ? m.Country.Name : null, m.ReceivedAt,
                m.Status, m.Priority,
                m.AssignedTo != null ? m.AssignedTo.FullName : null,
                m.Attachments.Count))
            .ToListAsync(ct);

        return new PagedResult<MaterialListItemDto>
        {
            Items = items, TotalCount = total, Page = r.Page, PageSize = r.PageSize
        };
    }
}
