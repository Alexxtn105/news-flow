using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Domain.Interfaces;
using NewsFlow.Infrastructure.Persistence;
using NewsFlow.Infrastructure.Persistence.Repositories;
using NewsFlow.Infrastructure.Services;

namespace NewsFlow.Infrastructure.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var dbProvider = configuration["DatabaseProvider"] ?? "PostgreSQL";
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<NewsFlowDbContext>(options =>
        {
            if (dbProvider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(
                    connectionString ?? "Data Source=newsflow.db",
                    b => b.MigrationsAssembly(typeof(NewsFlowDbContext).Assembly.FullName));
            }
            else
            {
                options.UseNpgsql(
                    connectionString,
                    b => b.MigrationsAssembly(typeof(NewsFlowDbContext).Assembly.FullName));
            }
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<NewsFlowDbContext>());
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<NewsFlowDbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IFileStorage>(new LocalFileStorage(
            configuration["Storage:BasePath"] ?? "storage"));
        services.AddHttpContextAccessor();

        return services;
    }
}
