using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Infrastructure.Yaml.Models;
using NewsFlow.Infrastructure.Yaml.Parsing;

namespace NewsFlow.Infrastructure.Yaml.Providers;

public class FilePipelineProvider : IPipelineProvider
{
    private readonly string _directory;
    private Dictionary<string, PipelineInfo> _cache = [];

    public FilePipelineProvider(string directory)
    {
        _directory = directory;
        Load();
    }

    public PipelineInfo? GetPipeline(string code)
        => _cache.GetValueOrDefault(code);

    public IReadOnlyList<PipelineInfo> GetAllPipelines()
        => _cache.Values.ToList();

    public void Reload() => Load();

    private void Load()
    {
        var result = new Dictionary<string, PipelineInfo>();

        if (!Directory.Exists(_directory))
        {
            _cache = result;
            return;
        }

        foreach (var file in Directory.GetFiles(_directory, "*.yaml"))
        {
            try
            {
                var def = YamlParser.ParsePipelineFromFile(file);
                result[def.Code] = MapToInfo(def);
            }
            catch (Exception)
            {
                // Skip invalid files
            }
        }

        _cache = result;
    }

    private static PipelineInfo MapToInfo(PipelineDefinition def) => new(
        def.Code,
        def.Name,
        def.Description,
        def.Entity,
        def.Version,
        def.Statuses.Select(s => new PipelineStatusInfo(s.Code, s.Name, s.Type, s.IsLocked, s.Color)).ToList(),
        def.Transitions.Select(t => new PipelineTransitionInfo(
            t.GetFromStatuses(),
            t.To,
            t.Action,
            t.Label,
            t.RequiredRole,
            t.LockToUser,
            t.OnlyAssignedUser,
            t.Unlock,
            t.RequiresFields,
            t.OptionalFields,
            t.CreatesVersion,
            t.LogMessage)).ToList());
}
