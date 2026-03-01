using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Application.Common;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.Materials.DTOs;
using NewsFlow.Domain.Enums;

namespace NewsFlow.Application.Workspaces.Queries;

public record GetWorkspaceQueueQuery(string WorkspaceCode, int Page = 1, int PageSize = 20)
    : IRequest<WorkspaceQueueResult>;

public record WorkspaceQueueResult(
    WorkspaceInfo Workspace,
    IReadOnlyList<object> Items,
    int TotalCount,
    int Page,
    int PageSize);

public class GetWorkspaceQueueHandler : IRequestHandler<GetWorkspaceQueueQuery, WorkspaceQueueResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IWorkspaceProvider _workspaces;

    public GetWorkspaceQueueHandler(IApplicationDbContext db, IWorkspaceProvider workspaces)
    {
        _db = db;
        _workspaces = workspaces;
    }

    public async Task<WorkspaceQueueResult> Handle(GetWorkspaceQueueQuery request, CancellationToken ct)
    {
        var workspace = _workspaces.GetWorkspace(request.WorkspaceCode)
            ?? throw new KeyNotFoundException($"Workspace '{request.WorkspaceCode}' not found");

        if (workspace.QueueEntity == "Material")
        {
            return await GetMaterialQueue(workspace, request, ct);
        }
        else if (workspace.QueueEntity == "Document")
        {
            return await GetDocumentQueue(workspace, request, ct);
        }

        throw new InvalidOperationException($"Unsupported entity type '{workspace.QueueEntity}'");
    }

    private async Task<WorkspaceQueueResult> GetMaterialQueue(
        WorkspaceInfo workspace, GetWorkspaceQueueQuery request, CancellationToken ct)
    {
        var query = _db.Materials
            .Include(m => m.OriginalLanguage)
            .Include(m => m.Source)
            .AsQueryable();

        // Apply filters from workspace config
        foreach (var (key, value) in workspace.QueueFilter)
        {
            query = key switch
            {
                "status" when value != null && Enum.TryParse<MaterialStatus>(value.ToString(), out var status) =>
                    query.Where(m => m.Status == status),
                "assigned_to" when value == null =>
                    query.Where(m => m.AssignedToId == null),
                _ => query
            };
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(m => m.Priority)
            .ThenBy(m => m.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new MaterialListItemDto(
                m.Id, m.Title,
                m.OriginalLanguage != null ? m.OriginalLanguage.Name : "",
                m.Source != null ? m.Source.Name : "",
                m.Country != null ? m.Country.Name : null,
                m.ReceivedAt, m.Status, m.Priority,
                null, m.Attachments.Count))
            .ToListAsync(ct);

        return new WorkspaceQueueResult(workspace, items.Cast<object>().ToList(), total, request.Page, request.PageSize);
    }

    private async Task<WorkspaceQueueResult> GetDocumentQueue(
        WorkspaceInfo workspace, GetWorkspaceQueueQuery request, CancellationToken ct)
    {
        var query = _db.Documents.AsQueryable();

        foreach (var (key, value) in workspace.QueueFilter)
        {
            query = key switch
            {
                "status" when value != null && Enum.TryParse<DocumentStatus>(value.ToString(), out var status) =>
                    query.Where(d => d.Status == status),
                "assigned_to" when value == null =>
                    query.Where(d => d.AssignedToId == null),
                _ => query
            };
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(d => d.Priority)
            .ThenBy(d => d.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new Documents.DTOs.DocumentListItemDto(
                d.Id, d.RegistrationNumber, d.Title, d.Status, d.Priority, null, d.CreatedAt))
            .ToListAsync(ct);

        return new WorkspaceQueueResult(workspace, items.Cast<object>().ToList(), total, request.Page, request.PageSize);
    }
}

