namespace NewsFlow.Application.Common.Interfaces;

public interface IDocxExportService
{
    byte[] ExportToDocx(string title, string content, string? registrationNumber = null);
}
