using NewsFlow.Domain.Common;
using NewsFlow.Domain.Enums;

namespace NewsFlow.Domain.Entities;

public class DocumentVersion : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public int VersionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; }
}
