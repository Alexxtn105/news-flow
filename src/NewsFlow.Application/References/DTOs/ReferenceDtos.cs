namespace NewsFlow.Application.References.DTOs;

public record SourceDto(Guid Id, string Name, string? Description, string? Type);
public record CreateSourceDto(string Name, string? Description, string? Type);
public record UpdateSourceDto(string? Name, string? Description, string? Type);

public record LanguageDto(Guid Id, string Code, string Name);
public record CreateLanguageDto(string Code, string Name);

public record CountryDto(Guid Id, string Code, string Name);
public record CreateCountryDto(string Code, string Name);

public record TagDto(Guid Id, string Name, string? Category);
public record CreateTagDto(string Name, string? Category);
public record UpdateTagDto(string? Name, string? Category);

public record RoleDto(Guid Id, string Name, string? Description);
