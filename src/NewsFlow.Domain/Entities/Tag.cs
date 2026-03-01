using NewsFlow.Domain.Common;

namespace NewsFlow.Domain.Entities;

public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
}
