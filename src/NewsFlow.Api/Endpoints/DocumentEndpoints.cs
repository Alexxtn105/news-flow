using MediatR;
using NewsFlow.Application.Documents.Commands;
using NewsFlow.Application.Documents.DTOs;
using NewsFlow.Application.Documents.Queries;
using NewsFlow.Domain.Enums;

namespace NewsFlow.Api.Endpoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documents").WithTags("Documents").RequireAuthorization();

        group.MapGet("/", async (int? page, int? pageSize, DocumentStatus? status, string? search, ISender s) =>
            Results.Ok(await s.Send(new GetDocumentsQuery(page ?? 1, pageSize ?? 20, status, search))));

        group.MapGet("/{id:guid}", async (Guid id, ISender s) =>
        {
            try { return Results.Ok(await s.Send(new GetDocumentByIdQuery(id))); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });

        group.MapPost("/", async (CreateDocumentDto dto, ISender s) =>
        {
            try
            {
                var result = await s.Send(new CreateDocumentCommand(dto));
                return Results.Created($"/api/documents/{result.Id}", result);
            }
            catch (KeyNotFoundException ex) { return Results.BadRequest(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });

        // Workflow actions
        MapWorkflowAction(group, "take-review", (id) => new TakeDocumentForReviewCommand(id));
        MapWorkflowAction(group, "approve-review", (id) => new ApproveReviewCommand(id));
        MapWorkflowAction(group, "take-registration", (id) => new TakeForRegistrationCommand(id));
        MapWorkflowAction(group, "take-control", (id) => new TakeForControlCommand(id));
        MapWorkflowAction(group, "approve-control", (id) => new ApproveControlCommand(id));
        MapWorkflowAction(group, "take-evaluation", (id) => new TakeForEvaluationCommand(id));

        group.MapPost("/{id:guid}/return-revision", async (Guid id, ReturnForRevisionCommand cmd, ISender s) =>
        {
            try { return Results.Ok(await s.Send(cmd with { DocumentId = id })); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });

        group.MapPost("/{id:guid}/complete-registration", async (Guid id, CompleteRegistrationCommand cmd, ISender s) =>
        {
            try { return Results.Ok(await s.Send(cmd with { DocumentId = id })); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });

        group.MapPost("/{id:guid}/complete-evaluation", async (Guid id, CompleteEvaluationCommand cmd, ISender s) =>
        {
            try { return Results.Ok(await s.Send(cmd with { DocumentId = id })); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });

        group.MapGet("/{id:guid}/export", async (Guid id, ISender s) =>
        {
            try
            {
                var result = await s.Send(new NewsFlow.Application.Documents.Queries.ExportDocumentQuery(id));
                return Results.File(result.FileContent,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    result.FileName);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        }).WithName("ExportDocument");
    }

    private static void MapWorkflowAction<TRequest>(RouteGroupBuilder group, string path, Func<Guid, TRequest> factory)
        where TRequest : IRequest<DocumentDto>
    {
        group.MapPost($"/{{id:guid}}/{path}", async (Guid id, ISender s) =>
        {
            try { return Results.Ok(await s.Send(factory(id))); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });
    }
}
