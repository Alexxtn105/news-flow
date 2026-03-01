using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Domain.Entities;
using NewsFlow.Domain.Enums;

namespace NewsFlow.Application.Distributions.Commands;

// === Distribute Document ===
public record DistributeDocumentCommand(Guid DocumentId, List<Guid> RecipientIds) : IRequest<List<DistributionDto>>;

public class DistributeDocumentHandler : IRequestHandler<DistributeDocumentCommand, List<DistributionDto>>
{
    private readonly IApplicationDbContext _db;
    public DistributeDocumentHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<DistributionDto>> Handle(DistributeDocumentCommand request, CancellationToken ct)
    {
        var doc = await _db.Documents.FindAsync([request.DocumentId], ct)
            ?? throw new KeyNotFoundException("Document not found");

        if (doc.Status != DocumentStatus.Evaluated)
            throw new InvalidOperationException("Only evaluated documents can be distributed");

        var recipients = await _db.Recipients
            .Where(r => request.RecipientIds.Contains(r.Id) && r.IsActive)
            .ToListAsync(ct);

        var distributions = new List<DocumentDistribution>();
        foreach (var recipient in recipients)
        {
            var dist = new DocumentDistribution
            {
                DocumentId = doc.Id,
                RecipientId = recipient.Id,
                Status = DistributionStatus.Sent,
                SentAt = DateTime.UtcNow
            };
            _db.DocumentDistributions.Add(dist);
            distributions.Add(dist);
        }

        await _db.SaveChangesAsync(ct);

        return distributions.Select(d => new DistributionDto(
            d.Id, d.DocumentId, d.RecipientId,
            recipients.First(r => r.Id == d.RecipientId).Name,
            d.Status, d.SentAt, d.EvaluationScore, d.EvaluationComment, d.EvaluatedAt))
            .ToList();
    }
}

// === Evaluate Distribution ===
public record EvaluateDistributionCommand(Guid DistributionId, int Score, string? Comment) : IRequest<DistributionDto>;

public class EvaluateDistributionHandler : IRequestHandler<EvaluateDistributionCommand, DistributionDto>
{
    private readonly IApplicationDbContext _db;
    public EvaluateDistributionHandler(IApplicationDbContext db) => _db = db;

    public async Task<DistributionDto> Handle(EvaluateDistributionCommand request, CancellationToken ct)
    {
        var dist = await _db.DocumentDistributions
            .Include(d => d.Recipient)
            .FirstOrDefaultAsync(d => d.Id == request.DistributionId, ct)
            ?? throw new KeyNotFoundException("Distribution not found");

        if (request.Score < 1 || request.Score > 5)
            throw new InvalidOperationException("Score must be 1-5");

        dist.EvaluationScore = request.Score;
        dist.EvaluationComment = request.Comment;
        dist.EvaluatedAt = DateTime.UtcNow;
        dist.Status = DistributionStatus.Evaluated;

        await _db.SaveChangesAsync(ct);

        return new DistributionDto(
            dist.Id, dist.DocumentId, dist.RecipientId, dist.Recipient.Name,
            dist.Status, dist.SentAt, dist.EvaluationScore, dist.EvaluationComment, dist.EvaluatedAt);
    }
}

// === DTOs ===
public record DistributionDto(
    Guid Id, Guid DocumentId, Guid RecipientId, string RecipientName,
    DistributionStatus Status, DateTime SentAt,
    int? EvaluationScore, string? EvaluationComment, DateTime? EvaluatedAt);

// === Recipients CRUD ===
public record RecipientDto(Guid Id, string Name, string? Department, string? Email, bool IsActive);
public record CreateRecipientDto(string Name, string? Department, string? Email);

public record CreateRecipientCommand(CreateRecipientDto Dto) : IRequest<RecipientDto>;
public class CreateRecipientHandler : IRequestHandler<CreateRecipientCommand, RecipientDto>
{
    private readonly IApplicationDbContext _db;
    public CreateRecipientHandler(IApplicationDbContext db) => _db = db;
    public async Task<RecipientDto> Handle(CreateRecipientCommand r, CancellationToken ct)
    {
        var entity = new Recipient { Name = r.Dto.Name, Department = r.Dto.Department, Email = r.Dto.Email };
        _db.Recipients.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new RecipientDto(entity.Id, entity.Name, entity.Department, entity.Email, entity.IsActive);
    }
}

public record GetRecipientsQuery : IRequest<IReadOnlyList<RecipientDto>>;
public class GetRecipientsHandler : IRequestHandler<GetRecipientsQuery, IReadOnlyList<RecipientDto>>
{
    private readonly IApplicationDbContext _db;
    public GetRecipientsHandler(IApplicationDbContext db) => _db = db;
    public async Task<IReadOnlyList<RecipientDto>> Handle(GetRecipientsQuery r, CancellationToken ct)
        => await _db.Recipients.OrderBy(x => x.Name)
            .Select(x => new RecipientDto(x.Id, x.Name, x.Department, x.Email, x.IsActive))
            .ToListAsync(ct);
}

public record GetDistributionsQuery(Guid DocumentId) : IRequest<IReadOnlyList<DistributionDto>>;
public class GetDistributionsHandler : IRequestHandler<GetDistributionsQuery, IReadOnlyList<DistributionDto>>
{
    private readonly IApplicationDbContext _db;
    public GetDistributionsHandler(IApplicationDbContext db) => _db = db;
    public async Task<IReadOnlyList<DistributionDto>> Handle(GetDistributionsQuery r, CancellationToken ct)
        => await _db.DocumentDistributions
            .Include(d => d.Recipient)
            .Where(d => d.DocumentId == r.DocumentId)
            .OrderBy(d => d.SentAt)
            .Select(d => new DistributionDto(
                d.Id, d.DocumentId, d.RecipientId, d.Recipient.Name,
                d.Status, d.SentAt, d.EvaluationScore, d.EvaluationComment, d.EvaluatedAt))
            .ToListAsync(ct);
}
