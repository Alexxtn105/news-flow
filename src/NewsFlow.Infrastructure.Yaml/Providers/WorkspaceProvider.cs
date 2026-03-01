using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Infrastructure.Yaml.Models;
using NewsFlow.Infrastructure.Yaml.Parsing;

namespace NewsFlow.Infrastructure.Yaml.Providers;

public class FileWorkspaceProvider : IWorkspaceProvider
{
    private readonly string _directory;
    private Dictionary<string, WorkspaceInfo> _cache = [];

    public FileWorkspaceProvider(string directory)
    {
        _directory = directory;
        Load();
    }

    public WorkspaceInfo? GetWorkspace(string code)
        => _cache.GetValueOrDefault(code);

    public IReadOnlyList<WorkspaceInfo> GetAllWorkspaces()
        => _cache.Values.ToList();

    public void Reload() => Load();

    private void Load()
    {
        var result = new Dictionary<string, WorkspaceInfo>();

        if (!Directory.Exists(_directory))
        {
            _cache = result;
            return;
        }

        foreach (var file in Directory.GetFiles(_directory, "*.yaml"))
        {
            try
            {
                var def = YamlParser.ParseWorkspaceFromFile(file);
                result[def.Code] = MapToInfo(def);
            }
            catch (Exception)
            {
                // Skip invalid files
            }
        }

        _cache = result;
    }

    private static WorkspaceInfo MapToInfo(WorkspaceDefinition def) => new(
        def.Code,
        def.Name,
        def.Description,
        def.RequiredRole,
        def.InputQueue.Entity,
        def.InputQueue.Filter,
        def.Actions.Select(a => new WorkspaceActionInfo(
            a.Key,
            a.Value.Label,
            a.Value.RequiresFields,
            a.Value.LogMessage)).ToList());
}
