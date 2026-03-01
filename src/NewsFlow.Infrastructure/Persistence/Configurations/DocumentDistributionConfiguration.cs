using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsFlow.Domain.Entities;

namespace NewsFlow.Infrastructure.Persistence.Configurations;

public class DocumentDistributionConfiguration : IEntityTypeConfiguration<DocumentDistribution>
{
    public void Configure(EntityTypeBuilder<DocumentDistribution> builder)
    {
        builder.HasKey(d => d.Id);
        builder.HasOne(d => d.Document).WithMany().HasForeignKey(d => d.DocumentId);
        builder.HasOne(d => d.Recipient).WithMany().HasForeignKey(d => d.RecipientId);
        builder.Property(d => d.EvaluationComment).HasMaxLength(2000);
    }
}
