using NewsFlow.Domain.Common;

namespace NewsFlow.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Details { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}
