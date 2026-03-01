using NewsFlow.Domain.Common;

namespace NewsFlow.Domain.Entities;

public class Recipient : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
}
