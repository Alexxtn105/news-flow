using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsFlow.Domain.Entities;

namespace NewsFlow.Infrastructure.Persistence.Configurations;

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("materials");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Title).HasMaxLength(500).IsRequired();
        builder.Property(m => m.Location).HasMaxLength(200);
        builder.Property(m => m.RejectionReason).HasMaxLength(1000);
        builder.HasIndex(m => m.Status);
        builder.HasIndex(m => m.AssignedToId);

        builder.HasOne(m => m.OriginalLanguage).WithMany().HasForeignKey(m => m.OriginalLanguageId);
        builder.HasOne(m => m.Source).WithMany().HasForeignKey(m => m.SourceId);
        builder.HasOne(m => m.Country).WithMany().HasForeignKey(m => m.CountryId);
        builder.HasOne(m => m.AssignedTo).WithMany().HasForeignKey(m => m.AssignedToId);
        builder.HasOne(m => m.CreatedBy).WithMany().HasForeignKey(m => m.CreatedById);

        builder.HasMany(m => m.Tags).WithMany()
            .UsingEntity("material_tags",
                l => l.HasOne(typeof(Tag)).WithMany().HasForeignKey("tag_id"),
                r => r.HasOne(typeof(Material)).WithMany().HasForeignKey("material_id"));

        builder.HasMany(m => m.Attachments).WithOne(a => a.Material).HasForeignKey(a => a.MaterialId);

        builder.Ignore(m => m.DomainEvents);
    }
}
