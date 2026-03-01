namespace NewsFlow.Application.Common.Interfaces;

public interface IPipelineProvider
{
    PipelineInfo? GetPipeline(string code);
    IReadOnlyList<PipelineInfo> GetAllPipelines();
    void Reload();
}

public interface IWorkspaceProvider
{
    WorkspaceInfo? GetWorkspace(string code);
    IReadOnlyList<WorkspaceInfo> GetAllWorkspaces();
    void Reload();
}

public record PipelineInfo(
    string Code,
    string Name,
    string? Description,
    string Entity,
    int Version,
    IReadOnlyList<PipelineStatusInfo> Statuses,
    IReadOnlyList<PipelineTransitionInfo> Transitions);

public record PipelineStatusInfo(
    string Code,
    string Name,
    string Type,
    bool IsLocked,
    string? Color);

public record PipelineTransitionInfo(
    IReadOnlyList<string> From,
    string To,
    string Action,
    string Label,
    string? RequiredRole,
    bool LockToUser,
    bool OnlyAssignedUser,
    bool Unlock,
    IReadOnlyList<string>? RequiresFields,
    IReadOnlyList<string>? OptionalFields,
    bool CreatesVersion,
    string? LogMessage);

public record WorkspaceInfo(
    string Code,
    string Name,
    string? Description,
    string RequiredRole,
    string QueueEntity,
    Dictionary<string, object?> QueueFilter,
    IReadOnlyList<WorkspaceActionInfo> Actions);

public record WorkspaceActionInfo(
    string Code,
    string Label,
    IReadOnlyList<string>? RequiresFields,
    string? LogMessage);
