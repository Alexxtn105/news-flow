using MediatR;
using NewsFlow.Application.AuditLogs.Queries;
using NewsFlow.Application.References.Commands;
using NewsFlow.Application.References.DTOs;
using NewsFlow.Application.References.Queries;

namespace NewsFlow.Api.Endpoints;

public static class ReferenceEndpoints
{
    public static void MapReferenceEndpoints(this IEndpointRouteBuilder app)
    {
        // Sources
        var sources = app.MapGroup("/api/admin/sources").WithTags("Sources").RequireAuthorization("AdminPolicy");
        sources.MapGet("/", async (ISender s) => Results.Ok(await s.Send(new GetSourcesQuery())));
        sources.MapPost("/", async (CreateSourceDto dto, ISender s) =>
        {
            var result = await s.Send(new CreateSourceCommand(dto));
            return Results.Created($"/api/admin/sources/{result.Id}", result);
        });
        sources.MapPut("/{id:guid}", async (Guid id, UpdateSourceDto dto, ISender s) =>
        {
            try { return Results.Ok(await s.Send(new UpdateSourceCommand(id, dto))); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });
        sources.MapDelete("/{id:guid}", async (Guid id, ISender s) =>
        {
            try { await s.Send(new DeleteSourceCommand(id)); return Results.NoContent(); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });

        // Languages
        var langs = app.MapGroup("/api/admin/languages").WithTags("Languages").RequireAuthorization("AdminPolicy");
        langs.MapGet("/", async (ISender s) => Results.Ok(await s.Send(new GetLanguagesQuery())));
        langs.MapPost("/", async (CreateLanguageDto dto, ISender s) =>
        {
            var result = await s.Send(new CreateLanguageCommand(dto));
            return Results.Created($"/api/admin/languages/{result.Id}", result);
        });
        langs.MapDelete("/{id:guid}", async (Guid id, ISender s) =>
        {
            try { await s.Send(new DeleteLanguageCommand(id)); return Results.NoContent(); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });

        // Countries
        var countries = app.MapGroup("/api/admin/countries").WithTags("Countries").RequireAuthorization("AdminPolicy");
        countries.MapGet("/", async (ISender s) => Results.Ok(await s.Send(new GetCountriesQuery())));
        countries.MapPost("/", async (CreateCountryDto dto, ISender s) =>
        {
            var result = await s.Send(new CreateCountryCommand(dto));
            return Results.Created($"/api/admin/countries/{result.Id}", result);
        });
        countries.MapDelete("/{id:guid}", async (Guid id, ISender s) =>
        {
            try { await s.Send(new DeleteCountryCommand(id)); return Results.NoContent(); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });

        // Tags
        var tags = app.MapGroup("/api/admin/tags").WithTags("Tags").RequireAuthorization("AdminPolicy");
        tags.MapGet("/", async (ISender s) => Results.Ok(await s.Send(new GetTagsQuery())));
        tags.MapPost("/", async (CreateTagDto dto, ISender s) =>
        {
            var result = await s.Send(new CreateTagCommand(dto));
            return Results.Created($"/api/admin/tags/{result.Id}", result);
        });
        tags.MapPut("/{id:guid}", async (Guid id, UpdateTagDto dto, ISender s) =>
        {
            try { return Results.Ok(await s.Send(new UpdateTagCommand(id, dto))); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });
        tags.MapDelete("/{id:guid}", async (Guid id, ISender s) =>
        {
            try { await s.Send(new DeleteTagCommand(id)); return Results.NoContent(); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });

        // Roles (read-only)
        var roles = app.MapGroup("/api/admin/roles").WithTags("Roles").RequireAuthorization("AdminPolicy");
        roles.MapGet("/", async (ISender s) => Results.Ok(await s.Send(new GetRolesQuery())));

        // Audit Logs
        var audit = app.MapGroup("/api/admin/audit-logs").WithTags("AuditLog").RequireAuthorization("AdminPolicy");
        audit.MapGet("/", async (int? page, int? pageSize, string? entityType, Guid? userId,
            DateTime? from, DateTime? to, ISender s) =>
            Results.Ok(await s.Send(new GetAuditLogsQuery(page ?? 1, pageSize ?? 50, entityType, userId, from, to))));
    }
}
