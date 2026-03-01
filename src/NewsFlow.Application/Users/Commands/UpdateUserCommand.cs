using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.Users.DTOs;
using NewsFlow.Domain.Entities;
using NewsFlow.Application.Common.Interfaces;

namespace NewsFlow.Application.Users.Commands;

public record UpdateUserCommand(Guid Id, UpdateUserDto Dto) : IRequest<UserDto>;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;

    public UpdateUserCommandHandler(IApplicationDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .Include(u => u.Languages)
            .FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new KeyNotFoundException("User not found");

        var dto = request.Dto;

        if (dto.FullName != null) user.FullName = dto.FullName;
        if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
        if (dto.Password != null) user.PasswordHash = _hasher.Hash(dto.Password);

        if (dto.RoleIds != null)
        {
            user.Roles = await _db.Roles.Where(r => dto.RoleIds.Contains(r.Id)).ToListAsync(ct);
        }

        if (dto.LanguageIds != null)
        {
            user.Languages = await _db.Languages.Where(l => dto.LanguageIds.Contains(l.Id)).ToListAsync(ct);
        }

        await _db.SaveChangesAsync(ct);

        return new UserDto(
            user.Id, user.Username, user.FullName, user.IsActive, user.CreatedAt, user.LastLoginAt,
            user.Roles.Select(r => r.Name).ToList(),
            user.Languages.Select(l => l.Code).ToList());
    }
}
