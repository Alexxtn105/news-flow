using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Auth.DTOs;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.Common.Interfaces;

namespace NewsFlow.Application.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenService _jwt;

    public RefreshTokenCommandHandler(IApplicationDbContext db, IJwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token");

        if (user.RefreshTokenExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired");

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
