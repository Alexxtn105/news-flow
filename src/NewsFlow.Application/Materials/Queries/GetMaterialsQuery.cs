using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.Materials.DTOs;
using NewsFlow.Domain.Enums;

namespace NewsFlow.Application.Materials.Queries;

public record GetMaterialsQuery(
    int Page = 1, int PageSize = 20, MaterialStatus? Status = null,
    string? Search = null, Guid? LanguageId = null, Priority? Priority = null)
    : IRequest<PagedResult<MaterialListItemDto>>;

public class GetMaterialsHandler : IRequestHandler<GetMaterialsQuery, PagedResult<MaterialListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public GetMaterialsHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<MaterialListItemDto>> Handle(GetMaterialsQuery r, CancellationToken ct)
    {
        var q = _db.Materials
            .Include(m => m.OriginalLanguage)
            .Include(m => m.Source)
            .Include(m => m.Country)
            .Include(m => m.AssignedTo)
            .AsQueryable();

        if (r.Status.HasValue) q = q.Where(m => m.Status == r.Status);
        if (r.LanguageId.HasValue) q = q.Where(m => m.OriginalLanguageId == r.LanguageId);
        if (r.Priority.HasValue) q = q.Where(m => m.Priority == r.Priority);
        if (!string.IsNullOrWhiteSpace(r.Search))
            q = q.Where(m => m.Title.ToLower().Contains(r.Search.ToLower()));

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(m => m.Priority).ThenBy(m => m.ReceivedAt)
            .Skip((r.Page - 1) * r.PageSize).Take(r.PageSize)
            .Select(m => new MaterialListItemDto(
                m.Id, m.Title, m.OriginalLanguage.Name, m.Source.Name,
                m.Country != null ? m.Country.Name : null, m.ReceivedAt,
                m.Status, m.Priority, m.AssignedTo != null ? m.AssignedTo.FullName : null,
                m.Attachments.Count))
            .ToListAsync(ct);

        return new PagedResult<MaterialListItemDto>
        {
            Items = items, TotalCount = total, Page = r.Page, PageSize = r.PageSize
        };
    }
}
