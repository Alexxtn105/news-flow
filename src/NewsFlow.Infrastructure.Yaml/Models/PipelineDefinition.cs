using YamlDotNet.Serialization;

namespace NewsFlow.Infrastructure.Yaml.Models;

public class PipelineYamlRoot
{
    [YamlMember(Alias = "pipeline")]
    public PipelineDefinition Pipeline { get; set; } = new();
}

public class PipelineDefinition
{
    [YamlMember(Alias = "code")]
    public string Code { get; set; } = "";

    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "entity")]
    public string Entity { get; set; } = "";

    [YamlMember(Alias = "version")]
    public int Version { get; set; } = 1;

    [YamlMember(Alias = "statuses")]
    public List<StatusDefinition> Statuses { get; set; } = [];

    [YamlMember(Alias = "transitions")]
    public List<TransitionDefinition> Transitions { get; set; } = [];

    [YamlMember(Alias = "lock_timeout")]
    public LockTimeoutConfig? LockTimeout { get; set; }
}

public class StatusDefinition
{
    [YamlMember(Alias = "code")]
    public string Code { get; set; } = "";

    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";

    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "intermediate"; // initial | intermediate | terminal | returned

    [YamlMember(Alias = "is_locked")]
    public bool IsLocked { get; set; }

    [YamlMember(Alias = "color")]
    public string? Color { get; set; }
}

public class TransitionDefinition
{
    [YamlMember(Alias = "from")]
    public object From { get; set; } = ""; // string or List<string>

    [YamlMember(Alias = "to")]
    public string To { get; set; } = "";

    [YamlMember(Alias = "action")]
    public string Action { get; set; } = "";

    [YamlMember(Alias = "label")]
    public string Label { get; set; } = "";

    [YamlMember(Alias = "required_role")]
    public string? RequiredRole { get; set; }

    [YamlMember(Alias = "lock_to_user")]
    public bool LockToUser { get; set; }

    [YamlMember(Alias = "only_assigned_user")]
    public bool OnlyAssignedUser { get; set; }

    [YamlMember(Alias = "unlock")]
    public bool Unlock { get; set; }

    [YamlMember(Alias = "requires_fields")]
    public List<string>? RequiresFields { get; set; }

    [YamlMember(Alias = "optional_fields")]
    public List<string>? OptionalFields { get; set; }

    [YamlMember(Alias = "update")]
    public Dictionary<string, object>? Update { get; set; }

    [YamlMember(Alias = "creates_child")]
    public CreatesChildConfig? CreatesChild { get; set; }

    [YamlMember(Alias = "creates_version")]
    public bool CreatesVersion { get; set; }

    [YamlMember(Alias = "log_message")]
    public string? LogMessage { get; set; }

    public IReadOnlyList<string> GetFromStatuses()
    {
        if (From is string s) return [s];
        if (From is List<object> list) return list.Select(x => x.ToString()!).ToList();
        return [From.ToString()!];
    }
}

public class CreatesChildConfig
{
    [YamlMember(Alias = "entity")]
    public string Entity { get; set; } = "";

    [YamlMember(Alias = "mapping")]
    public Dictionary<string, string> Mapping { get; set; } = [];
}

public class LockTimeoutConfig
{
    [YamlMember(Alias = "duration_hours")]
    public int DurationHours { get; set; } = 4;

    [YamlMember(Alias = "fallback_status")]
    public string? FallbackStatus { get; set; }

    [YamlMember(Alias = "notify")]
    public bool Notify { get; set; }
}
