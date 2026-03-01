using NewsFlow.Domain.Common;

namespace NewsFlow.Domain.Entities;

public class Attachment : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string BucketName { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
    public Guid MaterialId { get; set; }
    public Material Material { get; set; } = null!;
}
