using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsFlow.Domain.Entities;

namespace NewsFlow.Infrastructure.Persistence.Configurations;

public class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("languages");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Code).HasMaxLength(10).IsRequired();
        builder.HasIndex(l => l.Code).IsUnique();
        builder.Property(l => l.Name).HasMaxLength(100).IsRequired();
    }
}
