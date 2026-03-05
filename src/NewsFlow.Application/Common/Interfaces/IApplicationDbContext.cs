using Microsoft.EntityFrameworkCore;
using NewsFlow.Domain.Entities;

namespace NewsFlow.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Language> Languages { get; }
    DbSet<Country> Countries { get; }
    DbSet<Source> Sources { get; }
    DbSet<Tag> Tags { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Material> Materials { get; }
    DbSet<Attachment> Attachments { get; }
    DbSet<Document> Documents { get; }
    DbSet<DocumentMaterial> DocumentMaterials { get; }
    DbSet<DocumentComment> DocumentComments { get; }
    DbSet<DocumentVersion> DocumentVersions { get; }
    DbSet<Recipient> Recipients { get; }
    DbSet<DocumentDistribution> DocumentDistributions { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    void SetOriginalRowVersion(Domain.Common.BaseEntity entity, int rowVersion);
}
