using NewsFlow.Domain.Common;

namespace NewsFlow.Domain.Entities;

public class Language : BaseEntity
{
    public string Code { get; set; } = string.Empty; // ISO 639-1
    public string Name { get; set; } = string.Empty;
}
