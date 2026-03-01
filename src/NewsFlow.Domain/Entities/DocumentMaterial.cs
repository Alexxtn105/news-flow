using NewsFlow.Domain.Common;

namespace NewsFlow.Domain.Entities;

public class DocumentMaterial : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public Guid MaterialId { get; set; }
    public Material Material { get; set; } = null!;
}
