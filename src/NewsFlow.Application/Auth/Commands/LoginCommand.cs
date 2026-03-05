using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Auth.DTOs;
using NewsFlow.Application.Common.Interfaces;

namespace NewsFlow.Application.Auth.Commands;

public record LoginCommand(string Username, string Password) : IRequest<AuthResponse>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IPasswordHasher _hasher;

    public LoginCommandHandler(IApplicationDbContext db, IJwtTokenService jwt, IPasswordHasher hasher)
    {
        _db = db;
        _jwt = jwt;
        _hasher = hasher;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Username == request.Username, ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials");

        if (user.IsLockedOut)
            throw new UnauthorizedAccessException("Account is locked");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is inactive");

        if (!_hasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));
            await _db.SaveChangesAsync(ct);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        user.RecordSuccessfulLogin();

        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshToken = _jwt.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddHours(8);

        await _db.SaveChangesAsync(ct);

        return new AuthResponse(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(30),
            new UserInfo(user.Id, user.Username, user.FullName, user.Roles.Select(r => r.Name).ToList()));
    }
}
