using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.Users.DTOs;
using NewsFlow.Domain.Entities;

namespace NewsFlow.Application.Users.Commands;

public record CreateUserCommand(CreateUserDto Dto) : IRequest<UserDto>;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;

    public CreateUserCommandHandler(IApplicationDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        if (await _db.Users.AnyAsync(u => u.Username == dto.Username, ct))
            throw new InvalidOperationException($"Username '{dto.Username}' already exists");

        var roles = await _db.Roles.Where(r => dto.RoleIds.Contains(r.Id)).ToListAsync(ct);
        var languages = dto.LanguageIds != null
            ? await _db.Languages.Where(l => dto.LanguageIds.Contains(l.Id)).ToListAsync(ct)
            : [];

        var user = new User
        {
            Username = dto.Username,
            PasswordHash = _hasher.Hash(dto.Password),
            FullName = dto.FullName,
            Roles = roles,
            Languages = languages
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return ToDto(user);
    }

    private static UserDto ToDto(User u) => new(
        u.Id, u.Username, u.FullName, u.IsActive, u.CreatedAt, u.LastLoginAt,
        u.Roles.Select(r => r.Name).ToList(),
        u.Languages.Select(l => l.Code).ToList());
}
