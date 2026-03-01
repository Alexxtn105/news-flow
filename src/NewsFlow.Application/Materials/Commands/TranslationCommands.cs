using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.Materials.DTOs;

namespace NewsFlow.Application.Materials.Commands;

public record TakeMaterialForTranslationCommand(Guid MaterialId) : IRequest<MaterialDto>;
public class TakeForTranslationHandler : IRequestHandler<TakeMaterialForTranslationCommand, MaterialDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    public TakeForTranslationHandler(IApplicationDbContext db, ICurrentUserService cu) { _db = db; _currentUser = cu; }

    public async Task<MaterialDto> Handle(TakeMaterialForTranslationCommand r, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        var m = await _db.Materials
            .Include(m => m.OriginalLanguage).Include(m => m.Source).Include(m => m.Country)
            .Include(m => m.CreatedBy).Include(m => m.AssignedTo).Include(m => m.Attachments).Include(m => m.Tags)
            .FirstOrDefaultAsync(m => m.Id == r.MaterialId, ct)
            ?? throw new KeyNotFoundException("Material not found");
        m.TakeForTranslation(userId);
        await _db.SaveChangesAsync(ct);
        return CreateMaterialHandler.ToDto(m);
    }
}

public record SaveTranslationDraftCommand(Guid MaterialId, string TranslatedText) : IRequest;
public class SaveDraftHandler : IRequestHandler<SaveTranslationDraftCommand>
{
    private readonly IApplicationDbContext _db;
    public SaveDraftHandler(IApplicationDbContext db) => _db = db;
    public async Task Handle(SaveTranslationDraftCommand r, CancellationToken ct)
    {
        var m = await _db.Materials.FindAsync([r.MaterialId], ct) ?? throw new KeyNotFoundException();
        m.SaveTranslationDraft(r.TranslatedText);
        await _db.SaveChangesAsync(ct);
    }
}

public record CompleteTranslationCommand(Guid MaterialId, string TranslatedText) : IRequest<MaterialDto>;
public class CompleteTranslationHandler : IRequestHandler<CompleteTranslationCommand, MaterialDto>
{
    private readonly IApplicationDbContext _db;
    public CompleteTranslationHandler(IApplicationDbContext db) => _db = db;
    public async Task<MaterialDto> Handle(CompleteTranslationCommand r, CancellationToken ct)
    {
        var m = await _db.Materials
            .Include(m => m.OriginalLanguage).Include(m => m.Source).Include(m => m.Country)
            .Include(m => m.CreatedBy).Include(m => m.AssignedTo).Include(m => m.Attachments).Include(m => m.Tags)
            .FirstOrDefaultAsync(m => m.Id == r.MaterialId, ct)
            ?? throw new KeyNotFoundException();
        m.CompleteTranslation(r.TranslatedText);
        await _db.SaveChangesAsync(ct);
        return CreateMaterialHandler.ToDto(m);
    }
}

public record ReleaseMaterialFromTranslationCommand(Guid MaterialId) : IRequest;
public class ReleaseFromTranslationHandler : IRequestHandler<ReleaseMaterialFromTranslationCommand>
{
    private readonly IApplicationDbContext _db;
    public ReleaseFromTranslationHandler(IApplicationDbContext db) => _db = db;
    public async Task Handle(ReleaseMaterialFromTranslationCommand r, CancellationToken ct)
    {
        var m = await _db.Materials.FindAsync([r.MaterialId], ct) ?? throw new KeyNotFoundException();
        m.ReleaseFromTranslation();
        await _db.SaveChangesAsync(ct);
    }
}

public record RejectMaterialCommand(Guid MaterialId, string Reason) : IRequest;
public class RejectMaterialHandler : IRequestHandler<RejectMaterialCommand>
{
    private readonly IApplicationDbContext _db;
    public RejectMaterialHandler(IApplicationDbContext db) => _db = db;
    public async Task Handle(RejectMaterialCommand r, CancellationToken ct)
    {
        var m = await _db.Materials.FindAsync([r.MaterialId], ct) ?? throw new KeyNotFoundException();
        m.Reject(r.Reason);
        await _db.SaveChangesAsync(ct);
    }
}

// Translator queue
public record GetTranslatorQueueQuery(int Page = 1, int PageSize = 20)
    : IRequest<Application.Common.PagedResult<MaterialListItemDto>>;

public class GetTranslatorQueueHandler : IRequestHandler<GetTranslatorQueueQuery, Application.Common.PagedResult<MaterialListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    public GetTranslatorQueueHandler(IApplicationDbContext db, ICurrentUserService cu) { _db = db; _currentUser = cu; }

    public async Task<Application.Common.PagedResult<MaterialListItemDto>> Handle(GetTranslatorQueueQuery r, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        // Get user's languages
        var user = await _db.Users.Include(u => u.Languages).FirstOrDefaultAsync(u => u.Id == userId, ct);
        var langIds = user?.Languages.Select(l => l.Id).ToList() ?? [];

        var q = _db.Materials
            .Include(m => m.OriginalLanguage).Include(m => m.Source).Include(m => m.Country).Include(m => m.AssignedTo)
            .Where(m =>
                (m.Status == Domain.Enums.MaterialStatus.New || m.Status == Domain.Enums.MaterialStatus.ReturnedFromTranslation)
                && m.AssignedToId == null
                && m.OriginalLanguage.Code != "ru");

        if (langIds.Count > 0)
            q = q.Where(m => langIds.Contains(m.OriginalLanguageId));

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(m => m.Priority).ThenBy(m => m.ReceivedAt)
            .Skip((r.Page - 1) * r.PageSize).Take(r.PageSize)
            .Select(m => new MaterialListItemDto(
                m.Id, m.Title, m.OriginalLanguage.Name, m.Source.Name,
                m.Country != null ? m.Country.Name : null, m.ReceivedAt,
                m.Status, m.Priority, null, m.Attachments.Count))
            .ToListAsync(ct);

        return new Application.Common.PagedResult<MaterialListItemDto>
        {
            Items = items, TotalCount = total, Page = r.Page, PageSize = r.PageSize
        };
    }
}
