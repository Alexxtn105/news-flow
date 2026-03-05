namespace NewsFlow.Application.Common.Interfaces;

public class WorkspaceDefinitionDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string RequiredRole { get; set; } = "";
    public InputQueueDto InputQueue { get; set; } = new();
    public Dictionary<string, ActionDefinitionDto> Actions { get; set; } = [];
    public WorkLayoutDto? WorkLayout { get; set; }
}

public class InputQueueDto
{
    public string Entity { get; set; } = "";
    public Dictionary<string, object?> Filter { get; set; } = [];
    public List<SortConfigDto>? Sort { get; set; }
    public List<DisplayColumnDto>? DisplayColumns { get; set; }
}

public class SortConfigDto
{
    public string Field { get; set; } = "";
    public string Direction { get; set; } = "asc";
}

public class DisplayColumnDto
{
    public string Field { get; set; } = "";
    public string Label { get; set; } = "";
    public string? Width { get; set; }
    public string? Format { get; set; }
    public int? Truncate { get; set; }
}

public class ActionDefinitionDto
{
    public string Label { get; set; } = "";
    public List<string>? RequiresFields { get; set; }
    public string? CreatesEntity { get; set; }
    public string? LogMessage { get; set; }
}

public class WorkLayoutDto
{
    public string Type { get; set; } = "split-panel";
    public PanelConfigDto? Left { get; set; }
    public PanelConfigDto? Center { get; set; }
    public PanelConfigDto? Right { get; set; }
}

public class PanelConfigDto
{
    public string Title { get; set; } = "";
    public List<FieldConfigDto> Fields { get; set; } = [];
}

public class FieldConfigDto
{
    public string Field { get; set; } = "";
    public string? Type { get; set; }
    public string? Label { get; set; }
    public bool ReadOnly { get; set; }
    public bool Required { get; set; }
    public string? Placeholder { get; set; }
}
