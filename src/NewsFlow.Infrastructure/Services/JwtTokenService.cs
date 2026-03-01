using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Domain.Entities;

namespace NewsFlow.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("fullName", user.FullName),
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name));
        }

        var key = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(
                _config["Jwt:Secret"] ?? "NewsFlowSuperSecretKeyThatIsAtLeast32BytesLong!"));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expMinutes = int.Parse(_config["Jwt:AccessTokenExpirationMinutes"] ?? "30");

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "NewsFlow",
            audience: _config["Jwt:Audience"] ?? "NewsFlow",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public bool ValidateRefreshToken(string token)
    {
        return !string.IsNullOrWhiteSpace(token);
    }
}
