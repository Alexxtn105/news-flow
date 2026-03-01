using NewsFlow.Domain.Enums;

namespace NewsFlow.Application.Materials.DTOs;

public record MaterialDto(
    Guid Id, string Title, string? OriginalText, string? TranslatedText,
    string OriginalLanguage, string Source, string? Country, string? Location,
    DateTime ReceivedAt, DateTime? EventDate, MaterialStatus Status,
    Priority Priority, string? AssignedTo, DateTime CreatedAt, string CreatedBy,
    IReadOnlyList<AttachmentDto> Attachments, IReadOnlyList<string> Tags);

public record AttachmentDto(Guid Id, string FileName, string ContentType, long Size);

public record CreateMaterialDto(
    string Title, string? OriginalText, Guid OriginalLanguageId, Guid SourceId,
    Guid? CountryId, string? Location, DateTime? EventDate, Priority Priority,
    List<Guid>? TagIds);

public record MaterialListItemDto(
    Guid Id, string Title, string OriginalLanguage, string Source,
    string? Country, DateTime ReceivedAt, MaterialStatus Status,
    Priority Priority, string? AssignedTo, int AttachmentCount);
