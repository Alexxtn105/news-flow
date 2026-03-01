using NewsFlow.Infrastructure.Yaml.Models;

namespace NewsFlow.Infrastructure.Yaml.Validation;

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public static class PipelineValidator
{
    public static ValidationResult Validate(PipelineDefinition pipeline)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(pipeline.Code))
            result.Errors.Add("Pipeline code is required");

        if (string.IsNullOrWhiteSpace(pipeline.Entity))
            result.Errors.Add("Pipeline entity is required");

        if (pipeline.Statuses.Count == 0)
        {
            result.Errors.Add("Pipeline must have at least one status");
            return result;
        }

        var statusCodes = pipeline.Statuses.Select(s => s.Code).ToHashSet();

        // Check for unique status codes
        if (statusCodes.Count != pipeline.Statuses.Count)
            result.Errors.Add("Duplicate status codes found");

        // Check for initial status
        var initialStatuses = pipeline.Statuses.Where(s => s.Type == "initial").ToList();
        if (initialStatuses.Count == 0)
            result.Errors.Add("Pipeline must have at least one initial status");
        if (initialStatuses.Count > 1)
            result.Warnings.Add("Pipeline has multiple initial statuses");

        // Check for terminal status
        var terminalStatuses = pipeline.Statuses.Where(s => s.Type == "terminal").ToList();
        if (terminalStatuses.Count == 0)
            result.Warnings.Add("Pipeline has no terminal statuses");

        // Validate transitions
        foreach (var transition in pipeline.Transitions)
        {
            var fromStatuses = transition.GetFromStatuses();
            foreach (var from in fromStatuses)
            {
                if (!statusCodes.Contains(from))
                    result.Errors.Add($"Transition from unknown status '{from}'");
            }

            if (!statusCodes.Contains(transition.To))
                result.Errors.Add($"Transition to unknown status '{transition.To}'");
        }

        // Check reachability — all non-initial statuses must be reachable from transitions
        var reachable = new HashSet<string>(initialStatuses.Select(s => s.Code));
        bool changed;
        do
        {
            changed = false;
            foreach (var t in pipeline.Transitions)
            {
                if (t.GetFromStatuses().Any(f => reachable.Contains(f)) && reachable.Add(t.To))
                    changed = true;
            }
        } while (changed);

        var unreachable = statusCodes.Except(reachable).ToList();
        foreach (var u in unreachable)
            result.Warnings.Add($"Status '{u}' is not reachable from any initial status");

        // Check dead ends (non-terminal statuses with no outgoing transitions)
        var statusesWithOutgoing = pipeline.Transitions.SelectMany(t => t.GetFromStatuses()).ToHashSet();
        var terminalCodes = terminalStatuses.Select(s => s.Code).ToHashSet();
        foreach (var status in statusCodes)
        {
            if (!terminalCodes.Contains(status) && !statusesWithOutgoing.Contains(status))
                result.Warnings.Add($"Status '{status}' has no outgoing transitions and is not terminal (dead end)");
        }

        return result;
    }

    public static ValidationResult ValidateWorkspace(WorkspaceDefinition workspace)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(workspace.Code))
            result.Errors.Add("Workspace code is required");

        if (string.IsNullOrWhiteSpace(workspace.RequiredRole))
            result.Errors.Add("Workspace required_role is required");

        if (string.IsNullOrWhiteSpace(workspace.InputQueue.Entity))
            result.Errors.Add("Workspace input_queue.entity is required");

        if (workspace.Actions.Count == 0)
            result.Warnings.Add("Workspace has no actions defined");

        return result;
    }
}
