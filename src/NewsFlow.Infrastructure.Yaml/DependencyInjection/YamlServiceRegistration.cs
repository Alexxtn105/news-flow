using Microsoft.Extensions.DependencyInjection;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Infrastructure.Yaml.Providers;

namespace NewsFlow.Infrastructure.Yaml.DependencyInjection;

public static class YamlServiceRegistration
{
    public static IServiceCollection AddYamlConfiguration(
        this IServiceCollection services,
        string pipelinesDirectory,
        string workspacesDirectory)
    {
        services.AddSingleton<IPipelineProvider>(
            new FilePipelineProvider(pipelinesDirectory));
        services.AddSingleton<IWorkspaceProvider>(
            new FileWorkspaceProvider(workspacesDirectory));

        return services;
    }
}
