using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Infrastructure.Yaml.Models;
using NewsFlow.Infrastructure.Yaml.Parsing;

namespace NewsFlow.Infrastructure.Yaml.Providers;

public class FileWorkspaceProvider : IWorkspaceProvider
{
    private readonly string _directory;
    private readonly object _writeLock = new();
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

    public WorkspaceDefinitionDto? GetWorkspaceDefinition(string code)
    {
        var filePath = FindFile(code);
        if (filePath == null) return null;

        var def = YamlParser.ParseWorkspaceFromFile(filePath);
        return MapToDto(def);
    }

    public void SaveWorkspace(WorkspaceDefinitionDto definition)
    {
        lock (_writeLock)
        {
            var yamlModel = MapFromDto(definition);
            var yaml = YamlParser.SerializeWorkspace(yamlModel);

            Directory.CreateDirectory(_directory);
            var filePath = Path.Combine(_directory, $"{definition.Code}.yaml");
            File.WriteAllText(filePath, yaml);
            Load();
        }
    }

    public void DeleteWorkspace(string code)
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
                var def = YamlParser.ParseWorkspaceFromFile(file);
                if (def.Code == code) return file;
            }
            catch { }
        }
        return null;
    }

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

    private static WorkspaceDefinitionDto MapToDto(WorkspaceDefinition def) => new()
    {
        Code = def.Code,
        Name = def.Name,
        Description = def.Description,
        RequiredRole = def.RequiredRole,
        InputQueue = new InputQueueDto
        {
            Entity = def.InputQueue.Entity,
            Filter = new Dictionary<string, object?>(def.InputQueue.Filter),
            Sort = def.InputQueue.Sort?.Select(s => new SortConfigDto
            {
                Field = s.Field,
                Direction = s.Direction
            }).ToList(),
            DisplayColumns = def.InputQueue.DisplayColumns?.Select(c => new DisplayColumnDto
            {
                Field = c.Field,
                Label = c.Label,
                Width = c.Width,
                Format = c.Format,
                Truncate = c.Truncate
            }).ToList()
        },
        Actions = def.Actions.ToDictionary(
            a => a.Key,
            a => new ActionDefinitionDto
            {
                Label = a.Value.Label,
                RequiresFields = a.Value.RequiresFields,
                CreatesEntity = a.Value.CreatesEntity,
                LogMessage = a.Value.LogMessage
            }),
        WorkLayout = def.WorkLayout != null
            ? new WorkLayoutDto
            {
                Type = def.WorkLayout.Type,
                Left = MapPanelToDto(def.WorkLayout.Left),
                Center = MapPanelToDto(def.WorkLayout.Center),
                Right = MapPanelToDto(def.WorkLayout.Right)
            }
            : null
    };

    private static PanelConfigDto? MapPanelToDto(PanelConfig? panel)
    {
        if (panel == null) return null;
        return new PanelConfigDto
        {
            Title = panel.Title,
            Fields = panel.Fields.Select(f => new FieldConfigDto
            {
                Field = f.Field,
                Type = f.Type,
                Label = f.Label,
                ReadOnly = f.ReadOnly,
                Required = f.Required,
                Placeholder = f.Placeholder
            }).ToList()
        };
    }

    private static WorkspaceDefinition MapFromDto(WorkspaceDefinitionDto dto) => new()
    {
        Code = dto.Code,
        Name = dto.Name,
        Description = dto.Description,
        RequiredRole = dto.RequiredRole,
        InputQueue = new InputQueueConfig
        {
            Entity = dto.InputQueue.Entity,
            Filter = new Dictionary<string, object?>(dto.InputQueue.Filter),
            Sort = dto.InputQueue.Sort?.Select(s => new SortConfig
            {
                Field = s.Field,
                Direction = s.Direction
            }).ToList(),
            DisplayColumns = dto.InputQueue.DisplayColumns?.Select(c => new DisplayColumnConfig
            {
                Field = c.Field,
                Label = c.Label,
                Width = c.Width,
                Format = c.Format,
                Truncate = c.Truncate
            }).ToList()
        },
        Actions = dto.Actions.ToDictionary(
            a => a.Key,
            a => new ActionDefinition
            {
                Label = a.Value.Label,
                RequiresFields = a.Value.RequiresFields,
                CreatesEntity = a.Value.CreatesEntity,
                LogMessage = a.Value.LogMessage
            }),
        WorkLayout = dto.WorkLayout != null
            ? new WorkLayoutConfig
            {
                Type = dto.WorkLayout.Type,
                Left = MapPanelFromDto(dto.WorkLayout.Left),
                Center = MapPanelFromDto(dto.WorkLayout.Center),
                Right = MapPanelFromDto(dto.WorkLayout.Right)
            }
            : null
    };

    private static PanelConfig? MapPanelFromDto(PanelConfigDto? panel)
    {
        if (panel == null) return null;
        return new PanelConfig
        {
            Title = panel.Title,
            Fields = panel.Fields.Select(f => new FieldConfig
            {
                Field = f.Field,
                Type = f.Type,
                Label = f.Label,
                ReadOnly = f.ReadOnly,
                Required = f.Required,
                Placeholder = f.Placeholder
            }).ToList()
        };
    }
}
