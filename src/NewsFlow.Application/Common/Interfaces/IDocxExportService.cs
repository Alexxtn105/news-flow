using NewsFlow.Application.Documents.DTOs;

namespace NewsFlow.Application.Common.Interfaces;

public interface IDocxExportService
{
    byte[] ExportToDocx(DocumentDto document);
}
