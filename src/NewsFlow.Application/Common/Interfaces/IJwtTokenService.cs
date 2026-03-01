using NewsFlow.Domain.Entities;

namespace NewsFlow.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    bool ValidateRefreshToken(string token);
}
