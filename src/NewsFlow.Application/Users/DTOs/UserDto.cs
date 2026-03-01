namespace NewsFlow.Application.Users.DTOs;

public record UserDto(
    Guid Id,
    string Username,
    string FullName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Languages);

public record CreateUserDto(
    string Username,
    string Password,
    string FullName,
    List<Guid> RoleIds,
    List<Guid>? LanguageIds);

public record UpdateUserDto(
    string? FullName,
    bool? IsActive,
    string? Password,
    List<Guid>? RoleIds,
    List<Guid>? LanguageIds);
