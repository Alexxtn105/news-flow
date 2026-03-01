using NewsFlow.Domain.Common;

namespace NewsFlow.Domain.Entities;

public class DocumentComment : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
}
