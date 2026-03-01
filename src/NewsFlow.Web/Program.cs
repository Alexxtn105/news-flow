using MudBlazor.Services;
using NewsFlow.Application.DependencyInjection;
using NewsFlow.Infrastructure.DependencyInjection;
using NewsFlow.Infrastructure.Persistence;
using NewsFlow.Infrastructure.Yaml.DependencyInjection;
using NewsFlow.Web.Components;
using NewsFlow.Web.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddScoped<AuthStateService>();

var configPath = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "config", "workspaces");
builder.Services.AddYamlConfiguration(
    pipelinesDirectory: Path.Combine(configPath, "pipelines"),
    workspacesDirectory: configPath);

var app = builder.Build();

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
