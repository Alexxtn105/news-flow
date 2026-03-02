using Microsoft.EntityFrameworkCore;
using NewsFlow.Domain.Entities;

namespace NewsFlow.Infrastructure.Persistence;

public static class SeedData
{
    /// <summary>
    /// Runtime seed — only creates admin user if not exists.
    /// All reference data (roles, languages, countries, sources, tags)
    /// is seeded via EF migration SeedReferenceData.
    /// </summary>
    public static async Task SeedAsync(NewsFlowDbContext context)
    {
        // Admin user needs BCrypt at runtime, so it's handled here as fallback
        if (!await context.Users.AnyAsync())
        {
            // Ensure roles exist (they should from migration)
            if (!await context.Roles.AnyAsync())
                return; // migration hasn't run yet

            var allRoles = await context.Roles.ToListAsync();
            var admin = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                FullName = "Администратор системы",
                IsActive = true,
                Roles = allRoles
            };
            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}
