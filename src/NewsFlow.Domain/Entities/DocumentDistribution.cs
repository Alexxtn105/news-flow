using NewsFlow.Domain.Common;
using NewsFlow.Domain.Enums;

namespace NewsFlow.Domain.Entities;

public class DocumentDistribution : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public Guid RecipientId { get; set; }
    public Recipient Recipient { get; set; } = null!;
    public DistributionStatus Status { get; set; } = DistributionStatus.Sent;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public int? EvaluationScore { get; set; }
    public string? EvaluationComment { get; set; }
    public DateTime? EvaluatedAt { get; set; }
}
