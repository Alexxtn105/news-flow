using NewsFlow.Application.Common.Interfaces;

namespace NewsFlow.Infrastructure.Services;

/// <summary>
/// Placeholder DOCX export service.
/// Replace with DocumentFormat.OpenXml implementation when the NuGet package is available.
/// </summary>
public class DocxExportService : IDocxExportService
{
    public byte[] ExportToDocx(string title, string content, string? registrationNumber = null)
    {
        // TODO: Implement with DocumentFormat.OpenXml when package is added to ./packages/
        // For now, return a plain text file as a placeholder
        var text = $"{title}\n";
        if (!string.IsNullOrEmpty(registrationNumber))
            text += $"Рег. номер: {registrationNumber}\n";
        text += $"\n{content}";
        return System.Text.Encoding.UTF8.GetBytes(text);
    }
}
