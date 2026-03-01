using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsFlow.Domain.Entities;

namespace NewsFlow.Infrastructure.Persistence.Configurations;

public class SourceConfiguration : IEntityTypeConfiguration<Source>
{
    public void Configure(EntityTypeBuilder<Source> builder)
    {
        builder.ToTable("sources");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(s => s.Name).IsUnique();
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.Type).HasMaxLength(50);
    }
}
