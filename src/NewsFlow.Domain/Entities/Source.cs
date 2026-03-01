using NewsFlow.Domain.Common;

namespace NewsFlow.Domain.Entities;

public class Source : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }
}
