using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Domain.Common;
using NewsFlow.Domain.Entities;
using NewsFlow.Domain.Exceptions;
using NewsFlow.Domain.Interfaces;

namespace NewsFlow.Infrastructure.Persistence;

public class NewsFlowDbContext : DbContext, IUnitOfWork, IApplicationDbContext
{
    public NewsFlowDbContext(DbContextOptions<NewsFlowDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentMaterial> DocumentMaterials => Set<DocumentMaterial>();
    public DbSet<DocumentComment> DocumentComments => Set<DocumentComment>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<Recipient> Recipients => Set<Recipient>();
    public DbSet<DocumentDistribution> DocumentDistributions => Set<DocumentDistribution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NewsFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public void SetOriginalRowVersion(BaseEntity entity, int rowVersion)
    {
        Entry(entity).Property(e => e.RowVersion).OriginalValue = rowVersion;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified && entry.Entity is Material or Document)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                entry.Entity.RowVersion++;
            }
        }

        try
        {
            return await base.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var entry = ex.Entries.FirstOrDefault();
            var entityName = entry?.Entity.GetType().Name ?? "Unknown";
            var entityId = (entry?.Entity as BaseEntity)?.Id ?? Guid.Empty;
            throw new ConcurrencyConflictException(entityName, entityId, ex);
        }
    }
}
