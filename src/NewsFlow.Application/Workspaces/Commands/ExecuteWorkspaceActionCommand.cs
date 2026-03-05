using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Domain.Enums;

namespace NewsFlow.Application.Workspaces.Commands;

public record ExecuteWorkspaceActionCommand(
    string WorkspaceCode,
    string ActionCode,
    Guid EntityId,
    Dictionary<string, string>? InputFields = null,
    int? RowVersion = null) : IRequest<WorkspaceActionResult>;

public record WorkspaceActionResult(bool Success, string Message, Guid? EntityId = null);

public class ExecuteWorkspaceActionHandler : IRequestHandler<ExecuteWorkspaceActionCommand, WorkspaceActionResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IPipelineProvider _pipelines;
    private readonly IWorkspaceProvider _workspaces;
    private readonly ICurrentUserService _currentUser;

    public ExecuteWorkspaceActionHandler(
        IApplicationDbContext db,
        IPipelineProvider pipelines,
        IWorkspaceProvider workspaces,
        ICurrentUserService currentUser)
    {
        _db = db;
        _pipelines = pipelines;
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<WorkspaceActionResult> Handle(ExecuteWorkspaceActionCommand request, CancellationToken ct)
    {
        var workspace = _workspaces.GetWorkspace(request.WorkspaceCode)
            ?? throw new KeyNotFoundException($"Workspace '{request.WorkspaceCode}' not found");

        var action = workspace.Actions.FirstOrDefault(a => a.Code == request.ActionCode)
            ?? throw new KeyNotFoundException($"Action '{request.ActionCode}' not found in workspace '{request.WorkspaceCode}'");

        // Find the appropriate pipeline for the entity type
        var pipelines = _pipelines.GetAllPipelines()
            .Where(p => p.Entity == workspace.QueueEntity)
            .ToList();

        if (pipelines.Count == 0)
            throw new InvalidOperationException($"No pipeline found for entity '{workspace.QueueEntity}'");

        var pipeline = pipelines.First();

        // Find matching transition in pipeline
        if (workspace.QueueEntity == "Material")
        {
            return await HandleMaterialAction(request, pipeline, action, ct);
        }
        else if (workspace.QueueEntity == "Document")
        {
            return await HandleDocumentAction(request, pipeline, action, ct);
        }

        throw new InvalidOperationException($"Unsupported entity type '{workspace.QueueEntity}'");
    }

    private async Task<WorkspaceActionResult> HandleMaterialAction(
        ExecuteWorkspaceActionCommand request,
        PipelineInfo pipeline,
        WorkspaceActionInfo action,
        CancellationToken ct)
    {
        var material = await _db.Materials
            .FirstOrDefaultAsync(m => m.Id == request.EntityId, ct)
            ?? throw new KeyNotFoundException("Material not found");

        if (request.RowVersion.HasValue) _db.SetOriginalRowVersion(material, request.RowVersion.Value);
        var currentStatus = material.Status.ToString();

        // Find transition
        var transition = pipeline.Transitions
            .FirstOrDefault(t => t.Action == request.ActionCode && t.From.Contains(currentStatus));

        if (transition == null)
            throw new InvalidOperationException($"No transition for action '{request.ActionCode}' from status '{currentStatus}'");

        // Parse target status
        if (!Enum.TryParse<MaterialStatus>(transition.To, out var targetStatus))
            throw new InvalidOperationException($"Unknown material status '{transition.To}'");

        // Apply transition
        material.Status = targetStatus;

        if (transition.LockToUser)
        {
            material.AssignedToId = _currentUser.UserId;
            material.AssignedAt = DateTime.UtcNow;
        }
        else if (transition.Unlock)
        {
            material.AssignedToId = null;
            material.AssignedAt = null;
        }

        // Apply field updates from input
        if (request.InputFields != null)
        {
            if (request.InputFields.TryGetValue("translated_text", out var translatedText))
                material.TranslatedText = translatedText;
        }

        await _db.SaveChangesAsync(ct);

        return new WorkspaceActionResult(true, transition.LogMessage ?? "Action completed", material.Id);
    }

    private async Task<WorkspaceActionResult> HandleDocumentAction(
        ExecuteWorkspaceActionCommand request,
        PipelineInfo pipeline,
        WorkspaceActionInfo action,
        CancellationToken ct)
    {
        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == request.EntityId, ct)
            ?? throw new KeyNotFoundException("Document not found");

        if (request.RowVersion.HasValue) _db.SetOriginalRowVersion(document, request.RowVersion.Value);
        var currentStatus = document.Status.ToString();

        var transition = pipeline.Transitions
            .FirstOrDefault(t => t.Action == request.ActionCode && t.From.Contains(currentStatus));

        if (transition == null)
            throw new InvalidOperationException($"No transition for action '{request.ActionCode}' from status '{currentStatus}'");

        if (!Enum.TryParse<DocumentStatus>(transition.To, out var targetStatus))
            throw new InvalidOperationException($"Unknown document status '{transition.To}'");

        document.Status = targetStatus;

        if (transition.LockToUser)
        {
            document.AssignedToId = _currentUser.UserId;
            document.AssignedAt = DateTime.UtcNow;
        }
        else if (transition.Unlock)
        {
            document.AssignedToId = null;
            document.AssignedAt = null;
        }

        // Apply field updates
        if (request.InputFields != null)
        {
            if (request.InputFields.TryGetValue("registration_number", out var regNum))
                document.RegistrationNumber = regNum;

            if (request.InputFields.TryGetValue("comment", out var comment))
            {
                _db.DocumentComments.Add(new Domain.Entities.DocumentComment
                {
                    DocumentId = document.Id,
                    Text = comment,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // Create version snapshot if required
        if (transition.CreatesVersion)
        {
            _db.DocumentVersions.Add(new Domain.Entities.DocumentVersion
            {
                DocumentId = document.Id,
                Title = document.Title,
                Content = document.Content,
                VersionNumber = await _db.DocumentVersions.CountAsync(v => v.DocumentId == document.Id, ct) + 1,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);

        return new WorkspaceActionResult(true, transition.LogMessage ?? "Action completed", document.Id);
    }
}
