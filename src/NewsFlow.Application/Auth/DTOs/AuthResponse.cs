namespace NewsFlow.Application.Auth.DTOs;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfo User);

public record UserInfo(
    Guid Id,
    string Username,
    string FullName,
    IReadOnlyList<string> Roles);
