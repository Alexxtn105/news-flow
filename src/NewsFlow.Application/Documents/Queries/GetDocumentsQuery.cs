using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.Documents.DTOs;
using NewsFlow.Domain.Enums;

namespace NewsFlow.Application.Documents.Queries;

public record GetDocumentsQuery(
    int Page = 1, int PageSize = 20, DocumentStatus? Status = null, string? Search = null)
    : IRequest<PagedResult<DocumentListItemDto>>;

public class GetDocumentsHandler : IRequestHandler<GetDocumentsQuery, PagedResult<DocumentListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public GetDocumentsHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<DocumentListItemDto>> Handle(GetDocumentsQuery r, CancellationToken ct)
    {
        var q = _db.Documents.Include(d => d.AssignedTo).AsQueryable();
        if (r.Status.HasValue) q = q.Where(d => d.Status == r.Status);
        if (!string.IsNullOrWhiteSpace(r.Search))
            q = q.Where(d => d.Title.ToLower().Contains(r.Search.ToLower()));

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(d => d.Priority).ThenByDescending(d => d.CreatedAt)
            .Skip((r.Page - 1) * r.PageSize).Take(r.PageSize)
            .Select(d => new DocumentListItemDto(
                d.Id, d.RegistrationNumber, d.Title, d.Status, d.Priority,
                d.AssignedTo != null ? d.AssignedTo.FullName : null, d.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<DocumentListItemDto>
        { Items = items, TotalCount = total, Page = r.Page, PageSize = r.PageSize };
    }
}

public record GetDocumentByIdQuery(Guid Id) : IRequest<DocumentDto>;
public class GetDocumentByIdHandler : IRequestHandler<GetDocumentByIdQuery, DocumentDto>
{
    private readonly IApplicationDbContext _db;
    public GetDocumentByIdHandler(IApplicationDbContext db) => _db = db;
    public async Task<DocumentDto> Handle(GetDocumentByIdQuery r, CancellationToken ct)
    {
        var d = await _db.Documents
            .Include(d => d.CreatedBy).Include(d => d.AssignedTo)
            .Include(d => d.SourceMaterials).ThenInclude(sm => sm.Material)
            .Include(d => d.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(d => d.Id == r.Id, ct)
            ?? throw new KeyNotFoundException("Document not found");
        return CreateDocumentCommand.ToDto(d);
    }

    // Use the static helper from CreateDocumentHandler
    private static class CreateDocumentCommand
    {
        public static DocumentDto ToDto(Domain.Entities.Document d) =>
            Documents.Commands.CreateDocumentHandler.ToDto(d);
    }
}
