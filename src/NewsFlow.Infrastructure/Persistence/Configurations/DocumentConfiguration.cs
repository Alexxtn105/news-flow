using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsFlow.Domain.Entities;

namespace NewsFlow.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Title).HasMaxLength(500).IsRequired();
        builder.Property(d => d.RegistrationNumber).HasMaxLength(50);
        builder.Property(d => d.EvaluationCommentary).HasMaxLength(2000);
        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => d.RegistrationNumber).IsUnique().HasFilter("\"RegistrationNumber\" IS NOT NULL");

        builder.HasOne(d => d.AssignedTo).WithMany().HasForeignKey(d => d.AssignedToId);
        builder.HasOne(d => d.CreatedBy).WithMany().HasForeignKey(d => d.CreatedById);
        builder.HasMany(d => d.SourceMaterials).WithOne(dm => dm.Document).HasForeignKey(dm => dm.DocumentId);
        builder.HasMany(d => d.Comments).WithOne(c => c.Document).HasForeignKey(c => c.DocumentId);
        builder.HasMany(d => d.Versions).WithOne(v => v.Document).HasForeignKey(v => v.DocumentId);
        builder.Ignore(d => d.DomainEvents);
    }
}

public class DocumentMaterialConfiguration : IEntityTypeConfiguration<DocumentMaterial>
{
    public void Configure(EntityTypeBuilder<DocumentMaterial> builder)
    {
        builder.ToTable("document_materials");
        builder.HasKey(dm => dm.Id);
        builder.HasIndex(dm => new { dm.DocumentId, dm.MaterialId }).IsUnique();
        builder.HasOne(dm => dm.Material).WithMany().HasForeignKey(dm => dm.MaterialId);
    }
}

public class DocumentCommentConfiguration : IEntityTypeConfiguration<DocumentComment>
{
    public void Configure(EntityTypeBuilder<DocumentComment> builder)
    {
        builder.ToTable("document_comments");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Text).HasMaxLength(5000).IsRequired();
        builder.HasOne(c => c.Author).WithMany().HasForeignKey(c => c.AuthorId);
    }
}

public class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable("document_versions");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Title).HasMaxLength(500).IsRequired();
        builder.HasIndex(v => new { v.DocumentId, v.VersionNumber }).IsUnique();
    }
}
