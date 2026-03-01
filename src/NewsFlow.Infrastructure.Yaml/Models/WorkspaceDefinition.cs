using YamlDotNet.Serialization;

namespace NewsFlow.Infrastructure.Yaml.Models;

public class WorkspaceYamlRoot
{
    [YamlMember(Alias = "workspace")]
    public WorkspaceDefinition Workspace { get; set; } = new();
}

public class WorkspaceDefinition
{
    [YamlMember(Alias = "code")]
    public string Code { get; set; } = "";

    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "required_role")]
    public string RequiredRole { get; set; } = "";

    [YamlMember(Alias = "input_queue")]
    public InputQueueConfig InputQueue { get; set; } = new();

    [YamlMember(Alias = "actions")]
    public Dictionary<string, ActionDefinition> Actions { get; set; } = [];

    [YamlMember(Alias = "work_layout")]
    public WorkLayoutConfig? WorkLayout { get; set; }
}

public class InputQueueConfig
{
    [YamlMember(Alias = "entity")]
    public string Entity { get; set; } = "";

    [YamlMember(Alias = "filter")]
    public Dictionary<string, object?> Filter { get; set; } = [];

    [YamlMember(Alias = "sort")]
    public List<SortConfig>? Sort { get; set; }

    [YamlMember(Alias = "display_columns")]
    public List<DisplayColumnConfig>? DisplayColumns { get; set; }
}

public class SortConfig
{
    [YamlMember(Alias = "field")]
    public string Field { get; set; } = "";

    [YamlMember(Alias = "direction")]
    public string Direction { get; set; } = "asc";
}

public class DisplayColumnConfig
{
    [YamlMember(Alias = "field")]
    public string Field { get; set; } = "";

    [YamlMember(Alias = "label")]
    public string Label { get; set; } = "";

    [YamlMember(Alias = "width")]
    public string? Width { get; set; }

    [YamlMember(Alias = "format")]
    public string? Format { get; set; }

    [YamlMember(Alias = "truncate")]
    public int? Truncate { get; set; }
}

public class ActionDefinition
{
    [YamlMember(Alias = "label")]
    public string Label { get; set; } = "";

    [YamlMember(Alias = "requires_fields")]
    public List<string>? RequiresFields { get; set; }

    [YamlMember(Alias = "update")]
    public Dictionary<string, object?>? Update { get; set; }

    [YamlMember(Alias = "creates_entity")]
    public string? CreatesEntity { get; set; }

    [YamlMember(Alias = "mapping")]
    public Dictionary<string, object>? Mapping { get; set; }

    [YamlMember(Alias = "post_update")]
    public Dictionary<string, object?>? PostUpdate { get; set; }

    [YamlMember(Alias = "creates_child")]
    public CreatesChildConfig? CreatesChild { get; set; }

    [YamlMember(Alias = "log_message")]
    public string? LogMessage { get; set; }
}

public class WorkLayoutConfig
{
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "split-panel";

    [YamlMember(Alias = "left")]
    public PanelConfig? Left { get; set; }

    [YamlMember(Alias = "center")]
    public PanelConfig? Center { get; set; }

    [YamlMember(Alias = "right")]
    public PanelConfig? Right { get; set; }
}

public class PanelConfig
{
    [YamlMember(Alias = "title")]
    public string Title { get; set; } = "";

    [YamlMember(Alias = "fields")]
    public List<FieldConfig> Fields { get; set; } = [];
}

public class FieldConfig
{
    [YamlMember(Alias = "field")]
    public string Field { get; set; } = "";

    [YamlMember(Alias = "type")]
    public string? Type { get; set; }

    [YamlMember(Alias = "label")]
    public string? Label { get; set; }

    [YamlMember(Alias = "readonly")]
    public bool ReadOnly { get; set; }

    [YamlMember(Alias = "required")]
    public bool Required { get; set; }

    [YamlMember(Alias = "placeholder")]
    public string? Placeholder { get; set; }
}
