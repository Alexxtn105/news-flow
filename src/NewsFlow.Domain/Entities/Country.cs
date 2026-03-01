using NewsFlow.Domain.Common;

namespace NewsFlow.Domain.Entities;

public class Country : BaseEntity
{
    public string Code { get; set; } = string.Empty; // ISO 3166-1
    public string Name { get; set; } = string.Empty;
}
