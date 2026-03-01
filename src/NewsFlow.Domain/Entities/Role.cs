using NewsFlow.Domain.Common;

namespace NewsFlow.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<User> Users { get; set; } = [];
}
