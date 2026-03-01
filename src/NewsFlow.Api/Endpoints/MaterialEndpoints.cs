using MediatR;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.Materials.Commands;
using NewsFlow.Application.Materials.DTOs;
using NewsFlow.Application.Materials.Queries;
using NewsFlow.Domain.Entities;
using NewsFlow.Domain.Enums;

namespace NewsFlow.Api.Endpoints;

public static class MaterialEndpoints
{
    public static void MapMaterialEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/materials").WithTags("Materials").RequireAuthorization();

        group.MapGet("/", async (int? page, int? pageSize, MaterialStatus? status,
            string? search, Guid? languageId, Priority? priority, ISender sender) =>
            Results.Ok(await sender.Send(new GetMaterialsQuery(page ?? 1, pageSize ?? 20, status, search, languageId, priority))));

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            try { return Results.Ok(await sender.Send(new GetMaterialByIdQuery(id))); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });

        group.MapPost("/", async (CreateMaterialDto dto, ISender sender) =>
        {
            var result = await sender.Send(new CreateMaterialCommand(dto));
            return Results.Created($"/api/materials/{result.Id}", result);
        });

        // File upload
        group.MapPost("/{id:guid}/attachments", async (Guid id, IFormFileCollection files,
            IApplicationDbContext db, IFileStorage storage, CancellationToken ct) =>
        {
            var material = await db.Materials.FindAsync([id], ct);
            if (material == null) return Results.NotFound();

            var attachments = new List<AttachmentDto>();
            foreach (var file in files)
            {
                var objectKey = $"{id}/{Guid.NewGuid()}/{file.FileName}";
                using var stream = file.OpenReadStream();
                await storage.UploadAsync("newsflow", objectKey, stream, file.ContentType, ct);

                var attachment = new Attachment
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Size = file.Length,
                    BucketName = "newsflow",
                    ObjectKey = objectKey,
                    MaterialId = id
                };
                db.Attachments.Add(attachment);
                attachments.Add(new AttachmentDto(attachment.Id, attachment.FileName, attachment.ContentType, attachment.Size));
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(attachments);
        }).DisableAntiforgery();

        // File download
        group.MapGet("/{id:guid}/attachments/{aid:guid}/stream", async (Guid id, Guid aid,
            IApplicationDbContext db, IFileStorage storage, CancellationToken ct) =>
        {
            var attachment = await db.Attachments.FindAsync([aid], ct);
            if (attachment == null || attachment.MaterialId != id) return Results.NotFound();

            var stream = await storage.DownloadAsync(attachment.BucketName, attachment.ObjectKey, ct);
            return Results.File(stream, attachment.ContentType, attachment.FileName);
        });
    }
}
