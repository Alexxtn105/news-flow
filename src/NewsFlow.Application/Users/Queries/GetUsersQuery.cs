using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common;
using NewsFlow.Application.Users.DTOs;
using NewsFlow.Application.Common.Interfaces;

namespace NewsFlow.Application.Users.Queries;

public record GetUsersQuery(int Page = 1, int PageSize = 20, string? Search = null) : IRequest<PagedResult<UserDto>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private readonly IApplicationDbContext _db;

    public GetUsersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var query = _db.Users.Include(u => u.Roles).Include(u => u.Languages).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(search) ||
                u.FullName.ToLower().Contains(search));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.Username)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserDto(
                u.Id, u.Username, u.FullName, u.IsActive, u.CreatedAt, u.LastLoginAt,
                u.Roles.Select(r => r.Name).ToList(),
                u.Languages.Select(l => l.Code).ToList()))
            .ToListAsync(ct);

        return new PagedResult<UserDto>
        {
            Items = items,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
