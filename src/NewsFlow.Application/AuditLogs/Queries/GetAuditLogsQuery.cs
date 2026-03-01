using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common;
using NewsFlow.Application.Common.Interfaces;

namespace NewsFlow.Application.AuditLogs.Queries;

public record AuditLogDto(
    Guid Id, string Action, string EntityType, Guid? EntityId,
    Guid? UserId, string? UserName, string? Details, DateTime CreatedAt);

public record GetAuditLogsQuery(
    int Page = 1, int PageSize = 50, string? EntityType = null,
    Guid? UserId = null, DateTime? From = null, DateTime? To = null)
    : IRequest<PagedResult<AuditLogDto>>;

public class GetAuditLogsHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly IApplicationDbContext _db;
    public GetAuditLogsHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery r, CancellationToken ct)
    {
        var q = _db.AuditLogs.AsQueryable();
        if (r.EntityType != null) q = q.Where(a => a.EntityType == r.EntityType);
        if (r.UserId.HasValue) q = q.Where(a => a.UserId == r.UserId);
        if (r.From.HasValue) q = q.Where(a => a.CreatedAt >= r.From);
        if (r.To.HasValue) q = q.Where(a => a.CreatedAt <= r.To);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.CreatedAt)
            .Skip((r.Page - 1) * r.PageSize).Take(r.PageSize)
            .Select(a => new AuditLogDto(a.Id, a.Action, a.EntityType, a.EntityId, a.UserId, a.UserName, a.Details, a.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto> { Items = items, TotalCount = total, Page = r.Page, PageSize = r.PageSize };
    }
}
