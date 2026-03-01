using MediatR;
using NewsFlow.Application.Materials.Commands;

namespace NewsFlow.Api.Endpoints;

public static class TranslatorEndpoints
{
    public static void MapTranslatorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/materials").WithTags("Translator").RequireAuthorization();

        group.MapPost("/{id:guid}/take", async (Guid id, ISender sender) =>
        {
            try { return Results.Ok(await sender.Send(new TakeMaterialForTranslationCommand(id))); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });

        group.MapPost("/{id:guid}/translate", async (Guid id, SaveTranslationDraftCommand cmd, ISender sender) =>
        {
            try { await sender.Send(cmd with { MaterialId = id }); return Results.Ok(); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });

        group.MapPost("/{id:guid}/complete-translation", async (Guid id, CompleteTranslationCommand cmd, ISender sender) =>
        {
            try { return Results.Ok(await sender.Send(cmd with { MaterialId = id })); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });

        group.MapPost("/{id:guid}/release", async (Guid id, ISender sender) =>
        {
            try { await sender.Send(new ReleaseMaterialFromTranslationCommand(id)); return Results.Ok(); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });

        group.MapPost("/{id:guid}/reject", async (Guid id, RejectMaterialCommand cmd, ISender sender) =>
        {
            try { await sender.Send(cmd with { MaterialId = id }); return Results.Ok(); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });

        // Queue
        var ws = app.MapGroup("/api/workspaces/translator").WithTags("Translator").RequireAuthorization();
        ws.MapGet("/queue", async (int? page, int? pageSize, ISender sender) =>
            Results.Ok(await sender.Send(new GetTranslatorQueueQuery(page ?? 1, pageSize ?? 20))));
    }
}
