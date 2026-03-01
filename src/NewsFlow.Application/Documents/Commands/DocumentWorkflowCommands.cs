using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.Documents.DTOs;
using NewsFlow.Domain.Entities;

namespace NewsFlow.Application.Documents.Commands;

// === Review ===
public record TakeDocumentForReviewCommand(Guid DocumentId) : IRequest<DocumentDto>;
public class TakeForReviewHandler : IRequestHandler<TakeDocumentForReviewCommand, DocumentDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _cu;
    public TakeForReviewHandler(IApplicationDbContext db, ICurrentUserService cu) { _db = db; _cu = cu; }
    public async Task<DocumentDto> Handle(TakeDocumentForReviewCommand r, CancellationToken ct)
    {
        var userId = _cu.UserId ?? throw new UnauthorizedAccessException();
        var d = await LoadDoc(r.DocumentId, ct);
        d.TakeForReview(userId);
        await _db.SaveChangesAsync(ct);
        return CreateDocumentHandler.ToDto(d);
    }
    private async Task<Document> LoadDoc(Guid id, CancellationToken ct)
        => await _db.Documents.Include(d => d.CreatedBy).Include(d => d.AssignedTo)
            .Include(d => d.SourceMaterials).ThenInclude(sm => sm.Material)
            .Include(d => d.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(d => d.Id == id, ct) ?? throw new KeyNotFoundException();
}

public record ApproveReviewCommand(Guid DocumentId) : IRequest<DocumentDto>;
public class ApproveReviewHandler : IRequestHandler<ApproveReviewCommand, DocumentDto>
{
    private readonly IApplicationDbContext _db;
    public ApproveReviewHandler(IApplicationDbContext db) => _db = db;
    public async Task<DocumentDto> Handle(ApproveReviewCommand r, CancellationToken ct)
    {
        var d = await _db.Documents.Include(d => d.CreatedBy).Include(d => d.AssignedTo)
            .Include(d => d.SourceMaterials).ThenInclude(sm => sm.Material)
            .Include(d => d.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(d => d.Id == r.DocumentId, ct) ?? throw new KeyNotFoundException();
        d.ApproveReview();
        d.CreateVersionSnapshot();
        await _db.SaveChangesAsync(ct);
        return CreateDocumentHandler.ToDto(d);
    }
}

public record ReturnForRevisionCommand(Guid DocumentId, string Comment) : IRequest<DocumentDto>;
public class ReturnForRevisionHandler : IRequestHandler<ReturnForRevisionCommand, DocumentDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _cu;
    public ReturnForRevisionHandler(IApplicationDbContext db, ICurrentUserService cu) { _db = db; _cu = cu; }
    public async Task<DocumentDto> Handle(ReturnForRevisionCommand r, CancellationToken ct)
    {
        var userId = _cu.UserId ?? throw new UnauthorizedAccessException();
        var d = await _db.Documents.Include(d => d.CreatedBy).Include(d => d.AssignedTo)
            .Include(d => d.SourceMaterials).ThenInclude(sm => sm.Material)
            .Include(d => d.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(d => d.Id == r.DocumentId, ct) ?? throw new KeyNotFoundException();
        d.ReturnForRevision();
        d.Comments.Add(new DocumentComment { AuthorId = userId, Text = r.Comment });
        await _db.SaveChangesAsync(ct);
        return CreateDocumentHandler.ToDto(d);
    }
}

// === Registration ===
public record TakeForRegistrationCommand(Guid DocumentId) : IRequest<DocumentDto>;
public class TakeForRegistrationHandler : IRequestHandler<TakeForRegistrationCommand, DocumentDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _cu;
    public TakeForRegistrationHandler(IApplicationDbContext db, ICurrentUserService cu) { _db = db; _cu = cu; }
    public async Task<DocumentDto> Handle(TakeForRegistrationCommand r, CancellationToken ct)
    {
        var userId = _cu.UserId ?? throw new UnauthorizedAccessException();
        var d = await _db.Documents.Include(d => d.CreatedBy).Include(d => d.AssignedTo)
            .Include(d => d.SourceMaterials).ThenInclude(sm => sm.Material)
            .Include(d => d.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(d => d.Id == r.DocumentId, ct) ?? throw new KeyNotFoundException();
        d.TakeForRegistration(userId);
        await _db.SaveChangesAsync(ct);
        return CreateDocumentHandler.ToDto(d);
    }
}

public record CompleteRegistrationCommand(Guid DocumentId, string RegistrationNumber) : IRequest<DocumentDto>;
public class CompleteRegistrationHandler : IRequestHandler<CompleteRegistrationCommand, DocumentDto>
{
    private readonly IApplicationDbContext _db;
    public CompleteRegistrationHandler(IApplicationDbContext db) => _db = db;
    public async Task<DocumentDto> Handle(CompleteRegistrationCommand r, CancellationToken ct)
    {
        var d = await _db.Documents.Include(d => d.CreatedBy).Include(d => d.AssignedTo)
            .Include(d => d.SourceMaterials).ThenInclude(sm => sm.Material)
            .Include(d => d.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(d => d.Id == r.DocumentId, ct) ?? throw new KeyNotFoundException();
        d.CompleteRegistration(r.RegistrationNumber);
        d.CreateVersionSnapshot();
        await _db.SaveChangesAsync(ct);
        return CreateDocumentHandler.ToDto(d);
    }
}

// === Control ===
public record TakeForControlCommand(Guid DocumentId) : IRequest<DocumentDto>;
public class TakeForControlHandler : IRequestHandler<TakeForControlCommand, DocumentDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _cu;
    public TakeForControlHandler(IApplicationDbContext db, ICurrentUserService cu) { _db = db; _cu = cu; }
    public async Task<DocumentDto> Handle(TakeForControlCommand r, CancellationToken ct)
    {
        var userId = _cu.UserId ?? throw new UnauthorizedAccessException();
        var d = await _db.Documents.Include(d => d.CreatedBy).Include(d => d.AssignedTo)
            .Include(d => d.SourceMaterials).ThenInclude(sm => sm.Material)
            .Include(d => d.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(d => d.Id == r.DocumentId, ct) ?? throw new KeyNotFoundException();
        d.TakeForControl(userId);
        await _db.SaveChangesAsync(ct);
        return CreateDocumentHandler.ToDto(d);
    }
}

public record ApproveControlCommand(Guid DocumentId) : IRequest<DocumentDto>;
public class ApproveControlHandler : IRequestHandler<ApproveControlCommand, DocumentDto>
{
    private readonly IApplicationDbContext _db;
    public ApproveControlHandler(IApplicationDbContext db) => _db = db;
    public async Task<DocumentDto> Handle(ApproveControlCommand r, CancellationToken ct)
    {
        var d = await _db.Documents.Include(d => d.CreatedBy).Include(d => d.AssignedTo)
            .Include(d => d.SourceMaterials).ThenInclude(sm => sm.Material)
            .Include(d => d.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(d => d.Id == r.DocumentId, ct) ?? throw new KeyNotFoundException();
        d.ApproveControl();
        d.CreateVersionSnapshot();
        await _db.SaveChangesAsync(ct);
        return CreateDocumentHandler.ToDto(d);
    }
}

// === Evaluation ===
public record TakeForEvaluationCommand(Guid DocumentId) : IRequest<DocumentDto>;
public class TakeForEvaluationHandler : IRequestHandler<TakeForEvaluationCommand, DocumentDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _cu;
    public TakeForEvaluationHandler(IApplicationDbContext db, ICurrentUserService cu) { _db = db; _cu = cu; }
    public async Task<DocumentDto> Handle(TakeForEvaluationCommand r, CancellationToken ct)
    {
        var userId = _cu.UserId ?? throw new UnauthorizedAccessException();
        var d = await _db.Documents.Include(d => d.CreatedBy).Include(d => d.AssignedTo)
            .Include(d => d.SourceMaterials).ThenInclude(sm => sm.Material)
            .Include(d => d.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(d => d.Id == r.DocumentId, ct) ?? throw new KeyNotFoundException();
        d.TakeForEvaluation(userId);
        await _db.SaveChangesAsync(ct);
        return CreateDocumentHandler.ToDto(d);
    }
}

public record CompleteEvaluationCommand(Guid DocumentId, int Score, string? Commentary) : IRequest<DocumentDto>;
public class CompleteEvaluationHandler : IRequestHandler<CompleteEvaluationCommand, DocumentDto>
{
    private readonly IApplicationDbContext _db;
    public CompleteEvaluationHandler(IApplicationDbContext db) => _db = db;
    public async Task<DocumentDto> Handle(CompleteEvaluationCommand r, CancellationToken ct)
    {
        var d = await _db.Documents.Include(d => d.CreatedBy).Include(d => d.AssignedTo)
            .Include(d => d.SourceMaterials).ThenInclude(sm => sm.Material)
            .Include(d => d.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(d => d.Id == r.DocumentId, ct) ?? throw new KeyNotFoundException();
        d.CompleteEvaluation(r.Score, r.Commentary);
        d.CreateVersionSnapshot();
        await _db.SaveChangesAsync(ct);
        return CreateDocumentHandler.ToDto(d);
    }
}
