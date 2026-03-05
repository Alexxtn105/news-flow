using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Infrastructure.Yaml.Models;
using NewsFlow.Infrastructure.Yaml.Parsing;

namespace NewsFlow.Infrastructure.Yaml.Providers;

public class FilePipelineProvider : IPipelineProvider
{
    private readonly string _directory;
    private readonly object _writeLock = new();
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

    public PipelineDefinitionDto? GetPipelineDefinition(string code)
    {
        var filePath = FindFile(code);
        if (filePath == null) return null;

        var def = YamlParser.ParsePipelineFromFile(filePath);
        return MapToDto(def);
    }

    public void SavePipeline(PipelineDefinitionDto definition)
    {
        lock (_writeLock)
        {
            var yamlModel = MapFromDto(definition);
            var yaml = YamlParser.SerializePipeline(yamlModel);

            Directory.CreateDirectory(_directory);
            var filePath = Path.Combine(_directory, $"{definition.Code}.yaml");
            File.WriteAllText(filePath, yaml);
            Load();
        }
    }

    public void DeletePipeline(string code)
    {
        lock (_writeLock)
        {
            var filePath = FindFile(code);
            if (filePath != null)
            {
                File.Delete(filePath);
                Load();
            }
        }
    }

    public void Reload() => Load();

    private string? FindFile(string code)
    {
        if (!Directory.Exists(_directory)) return null;

        foreach (var file in Directory.GetFiles(_directory, "*.yaml"))
        {
            try
            {
                var def = YamlParser.ParsePipelineFromFile(file);
                if (def.Code == code) return file;
            }
            catch { }
        }
        return null;
    }

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

    private static PipelineDefinitionDto MapToDto(PipelineDefinition def) => new()
    {
        Code = def.Code,
        Name = def.Name,
        Description = def.Description,
        Entity = def.Entity,
        Version = def.Version,
        Statuses = def.Statuses.Select(s => new StatusDefinitionDto
        {
            Code = s.Code,
            Name = s.Name,
            Type = s.Type,
            IsLocked = s.IsLocked,
            Color = s.Color
        }).ToList(),
        Transitions = def.Transitions.Select(t => new TransitionDefinitionDto
        {
            From = t.GetFromStatuses().ToList(),
            To = t.To,
            Action = t.Action,
            Label = t.Label,
            RequiredRole = t.RequiredRole,
            LockToUser = t.LockToUser,
            OnlyAssignedUser = t.OnlyAssignedUser,
            Unlock = t.Unlock,
            RequiresFields = t.RequiresFields,
            OptionalFields = t.OptionalFields,
            CreatesVersion = t.CreatesVersion,
            LogMessage = t.LogMessage
        }).ToList(),
        LockTimeout = def.LockTimeout != null
            ? new LockTimeoutDto
            {
                DurationHours = def.LockTimeout.DurationHours,
                FallbackStatus = def.LockTimeout.FallbackStatus,
                Notify = def.LockTimeout.Notify
            }
            : null
    };

    private static PipelineDefinition MapFromDto(PipelineDefinitionDto dto) => new()
    {
        Code = dto.Code,
        Name = dto.Name,
        Description = dto.Description,
        Entity = dto.Entity,
        Version = dto.Version,
        Statuses = dto.Statuses.Select(s => new StatusDefinition
        {
            Code = s.Code,
            Name = s.Name,
            Type = s.Type,
            IsLocked = s.IsLocked,
            Color = s.Color
        }).ToList(),
        Transitions = dto.Transitions.Select(t => new TransitionDefinition
        {
            From = t.From.Count == 1 ? (object)t.From[0] : t.From.Cast<object>().ToList(),
            To = t.To,
            Action = t.Action,
            Label = t.Label,
            RequiredRole = t.RequiredRole,
            LockToUser = t.LockToUser,
            OnlyAssignedUser = t.OnlyAssignedUser,
            Unlock = t.Unlock,
            RequiresFields = t.RequiresFields,
            OptionalFields = t.OptionalFields,
            CreatesVersion = t.CreatesVersion,
            LogMessage = t.LogMessage
        }).ToList(),
        LockTimeout = dto.LockTimeout != null
            ? new LockTimeoutConfig
            {
                DurationHours = dto.LockTimeout.DurationHours,
                FallbackStatus = dto.LockTimeout.FallbackStatus,
                Notify = dto.LockTimeout.Notify
            }
            : null
    };
}
