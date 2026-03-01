using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.Documents.DTOs;
using NewsFlow.Domain.Entities;

namespace NewsFlow.Application.Documents.Commands;

public record CreateDocumentCommand(CreateDocumentDto Dto) : IRequest<DocumentDto>;

public class CreateDocumentHandler : IRequestHandler<CreateDocumentCommand, DocumentDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateDocumentHandler(IApplicationDbContext db, ICurrentUserService cu) { _db = db; _currentUser = cu; }

    public async Task<DocumentDto> Handle(CreateDocumentCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        var dto = request.Dto;

        var doc = new Document
        {
            Title = dto.Title,
            Content = dto.Content,
            Priority = dto.Priority,
            CreatedById = userId,
        };

        foreach (var matId in dto.MaterialIds)
        {
            var material = await _db.Materials.FindAsync([matId], ct)
                ?? throw new KeyNotFoundException($"Material {matId} not found");
            material.CompleteAnalysis();
            doc.SourceMaterials.Add(new DocumentMaterial { MaterialId = matId });
        }

        _db.Documents.Add(doc);
        await _db.SaveChangesAsync(ct);

        return await LoadDto(doc.Id, ct);
    }

    internal async Task<DocumentDto> LoadDto(Guid id, CancellationToken ct)
    {
        var d = await _db.Documents
            .Include(d => d.CreatedBy).Include(d => d.AssignedTo)
            .Include(d => d.SourceMaterials).ThenInclude(sm => sm.Material)
            .Include(d => d.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new KeyNotFoundException("Document not found");

        return ToDto(d);
    }

    internal static DocumentDto ToDto(Document d) => new(
        d.Id, d.RegistrationNumber, d.Title, d.Content,
        d.Status, d.Priority, d.AssignedTo?.FullName,
        d.CreatedBy?.FullName ?? "", d.CreatedAt,
        d.EvaluationScore, d.EvaluationCommentary,
        d.SourceMaterials.Select(sm => new DocumentMaterialRefDto(sm.MaterialId, sm.Material?.Title ?? "")).ToList(),
        d.Comments.Select(c => new DocumentCommentDto(c.Id, c.Author?.FullName ?? "", c.Text, c.CreatedAt)).ToList());
}
