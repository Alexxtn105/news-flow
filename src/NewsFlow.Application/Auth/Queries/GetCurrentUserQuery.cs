using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Auth.DTOs;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.Common.Interfaces;

namespace NewsFlow.Application.Auth.Queries;

public record GetCurrentUserQuery : IRequest<UserInfo>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserInfo>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentUserQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<UserInfo> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Not authenticated");

        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new KeyNotFoundException("User not found");

        return new UserInfo(user.Id, user.Username, user.FullName, user.Roles.Select(r => r.Name).ToList());
    }
}
