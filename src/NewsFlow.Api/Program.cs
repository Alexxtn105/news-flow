using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NewsFlow.Api.Endpoints;
using NewsFlow.Api.Hubs;
using NewsFlow.Api.Services;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.DependencyInjection;
using NewsFlow.Infrastructure;
using NewsFlow.Infrastructure.DependencyInjection;
using NewsFlow.Infrastructure.Persistence;
using NewsFlow.Infrastructure.Yaml.DependencyInjection;
using Serilog;

EnvFileLoader.Load();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/newsflow-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting NewsFlow API");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // Services
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    var configPath = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "config", "workspaces");
    builder.Services.AddYamlConfiguration(
        pipelinesDirectory: Path.Combine(configPath, "pipelines"),
        workspacesDirectory: configPath);

    // JWT Authentication
    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret is not configured. Set JWT_SECRET in .env file.");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "NewsFlow",
                ValidAudience = builder.Configuration["Jwt:Audience"] ?? "NewsFlow",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Administrator"));
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "NewsFlow API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new()
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
        c.AddSecurityRequirement(new()
        {
            {
                new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
                []
            }
        });
    });

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        });
    });

    builder.Services.AddSignalR();
    builder.Services.AddSingleton<INotificationService, SignalRNotificationService>();

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<NewsFlowDbContext>();

    var app = builder.Build();

    // Migrate & seed
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<NewsFlowDbContext>();
        var dbProvider = builder.Configuration["DatabaseProvider"] ?? "PostgreSQL";
        if (dbProvider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
        {
            await db.Database.EnsureCreatedAsync();
        }
        else
        {
            await db.Database.MigrateAsync();
        }
        await SeedData.SeedAsync(db);
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<NewsFlow.Api.Middleware.ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health");
    app.MapGet("/", () => Results.Redirect("/swagger"));
    app.MapGet("/api/ping", () => Results.Ok(new { message = "pong", timestamp = DateTime.UtcNow }))
        .WithTags("System");

    app.MapAuthEndpoints();
    app.MapUserEndpoints();
    app.MapReferenceEndpoints();
    app.MapMaterialEndpoints();
    app.MapTranslatorEndpoints();
    app.MapDocumentEndpoints();
    app.MapWorkspaceEndpoints();
    app.MapDistributionEndpoints();
    app.MapHub<NotificationHub>("/hubs/notifications");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
