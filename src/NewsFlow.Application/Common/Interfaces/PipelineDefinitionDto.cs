namespace NewsFlow.Application.Common.Interfaces;

public class PipelineDefinitionDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Entity { get; set; } = "Material";
    public int Version { get; set; } = 1;
    public List<StatusDefinitionDto> Statuses { get; set; } = [];
    public List<TransitionDefinitionDto> Transitions { get; set; } = [];
    public LockTimeoutDto? LockTimeout { get; set; }
}

public class StatusDefinitionDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "intermediate";
    public bool IsLocked { get; set; }
    public string? Color { get; set; }
}

public class TransitionDefinitionDto
{
    public List<string> From { get; set; } = [];
    public string To { get; set; } = "";
    public string Action { get; set; } = "";
    public string Label { get; set; } = "";
    public string? RequiredRole { get; set; }
    public bool LockToUser { get; set; }
    public bool OnlyAssignedUser { get; set; }
    public bool Unlock { get; set; }
    public List<string>? RequiresFields { get; set; }
    public List<string>? OptionalFields { get; set; }
    public bool CreatesVersion { get; set; }
    public string? LogMessage { get; set; }
}

public class LockTimeoutDto
{
    public int DurationHours { get; set; } = 4;
    public string? FallbackStatus { get; set; }
    public bool Notify { get; set; }
}
