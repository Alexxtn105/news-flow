using MediatR;
using NewsFlow.Application.Workspaces.Commands;
using NewsFlow.Application.Workspaces.Queries;

namespace NewsFlow.Api.Endpoints;

public static class WorkspaceEndpoints
{
    public static void MapWorkspaceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/workspaces").WithTags("Workspaces");

        group.MapGet("/", async (ISender sender) =>
        {
            var workspaces = await sender.Send(new GetWorkspacesQuery());
            return Results.Ok(workspaces);
        });

        group.MapGet("/pipelines", async (ISender sender) =>
        {
            var pipelines = await sender.Send(new GetPipelinesQuery());
            return Results.Ok(pipelines);
        });

        group.MapGet("/{code}/queue", async (string code, int page, int pageSize, ISender sender) =>
        {
            var result = await sender.Send(new GetWorkspaceQueueQuery(code, page > 0 ? page : 1, pageSize > 0 ? pageSize : 20));
            return Results.Ok(result);
        });

        group.MapPost("/{code}/action/{actionCode}", async (
            string code, string actionCode, Guid entityId,
            Dictionary<string, string>? fields, ISender sender) =>
        {
            var result = await sender.Send(new ExecuteWorkspaceActionCommand(code, actionCode, entityId, fields));
            return Results.Ok(result);
        });
    }
}
