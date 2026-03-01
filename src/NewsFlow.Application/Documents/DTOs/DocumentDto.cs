using NewsFlow.Domain.Enums;

namespace NewsFlow.Application.Documents.DTOs;

public record DocumentDto(
    Guid Id, string? RegistrationNumber, string Title, string Content,
    DocumentStatus Status, Priority Priority, string? AssignedTo,
    string CreatedBy, DateTime CreatedAt,
    int? EvaluationScore, string? EvaluationCommentary,
    IReadOnlyList<DocumentMaterialRefDto> SourceMaterials,
    IReadOnlyList<DocumentCommentDto> Comments);

public record DocumentMaterialRefDto(Guid MaterialId, string Title);
public record DocumentCommentDto(Guid Id, string Author, string Text, DateTime CreatedAt);

public record DocumentListItemDto(
    Guid Id, string? RegistrationNumber, string Title,
    DocumentStatus Status, Priority Priority, string? AssignedTo, DateTime CreatedAt);

public record CreateDocumentDto(
    string Title, string Content, List<Guid> MaterialIds, Priority Priority);
