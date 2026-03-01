using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsFlow.Domain.Entities;

namespace NewsFlow.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Username).HasMaxLength(100).IsRequired();
        builder.HasIndex(u => u.Username).IsUnique();
        builder.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.RefreshToken).HasMaxLength(500);

        builder.HasMany(u => u.Roles)
            .WithMany(r => r.Users)
            .UsingEntity("user_roles",
                l => l.HasOne(typeof(Role)).WithMany().HasForeignKey("role_id"),
                r => r.HasOne(typeof(User)).WithMany().HasForeignKey("user_id"));

        builder.HasMany(u => u.Languages)
            .WithMany()
            .UsingEntity("user_languages",
                l => l.HasOne(typeof(Language)).WithMany().HasForeignKey("language_id"),
                r => r.HasOne(typeof(User)).WithMany().HasForeignKey("user_id"));

        builder.Ignore(u => u.DomainEvents);
    }
}
