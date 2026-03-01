using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Users.DTOs;
using NewsFlow.Application.Common.Interfaces;

namespace NewsFlow.Application.Users.Queries;

public record GetUserByIdQuery(Guid Id) : IRequest<UserDto>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly IApplicationDbContext _db;

    public GetUserByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .Include(u => u.Languages)
            .FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new KeyNotFoundException("User not found");

        return new UserDto(
            user.Id, user.Username, user.FullName, user.IsActive, user.CreatedAt, user.LastLoginAt,
            user.Roles.Select(r => r.Name).ToList(),
            user.Languages.Select(l => l.Code).ToList());
    }
}
