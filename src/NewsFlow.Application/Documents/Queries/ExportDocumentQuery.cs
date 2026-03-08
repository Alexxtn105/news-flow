using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.Documents.Commands;

namespace NewsFlow.Application.Documents.Queries;

public record ExportDocumentQuery(Guid DocumentId) : IRequest<ExportDocumentResult>;
public record ExportDocumentResult(byte[] FileContent, string FileName);

public class ExportDocumentHandler : IRequestHandler<ExportDocumentQuery, ExportDocumentResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IDocxExportService _export;

    public ExportDocumentHandler(IApplicationDbContext db, IDocxExportService export)
    {
        _db = db;
        _export = export;
    }

    public async Task<ExportDocumentResult> Handle(ExportDocumentQuery r, CancellationToken ct)
    {
        var d = await _db.Documents
            .Include(d => d.CreatedBy).Include(d => d.AssignedTo)
            .Include(d => d.SourceMaterials).ThenInclude(sm => sm.Material)
            .Include(d => d.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(d => d.Id == r.DocumentId, ct)
            ?? throw new KeyNotFoundException("Document not found");

        var dto = CreateDocumentHandler.ToDto(d);
        var bytes = _export.ExportToDocx(dto);

        var safeName = string.Join("_", d.Title.Split(Path.GetInvalidFileNameChars()));
        if (safeName.Length > 50) safeName = safeName[..50];
        var fileName = $"{safeName}.docx";

        return new ExportDocumentResult(bytes, fileName);
    }
}
