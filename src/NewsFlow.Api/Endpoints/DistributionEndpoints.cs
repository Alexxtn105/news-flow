using MediatR;
using NewsFlow.Application.Distributions.Commands;

namespace NewsFlow.Api.Endpoints;

public static class DistributionEndpoints
{
    public static void MapDistributionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithTags("Distributions");

        group.MapGet("/recipients", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetRecipientsQuery())));

        group.MapPost("/recipients", async (CreateRecipientDto dto, ISender sender) =>
            Results.Ok(await sender.Send(new CreateRecipientCommand(dto))));

        group.MapPost("/documents/{id:guid}/distribute", async (Guid id, List<Guid> recipientIds, ISender sender) =>
            Results.Ok(await sender.Send(new DistributeDocumentCommand(id, recipientIds))));

        group.MapGet("/documents/{id:guid}/distributions", async (Guid id, ISender sender) =>
            Results.Ok(await sender.Send(new GetDistributionsQuery(id))));

        group.MapPost("/distributions/{id:guid}/evaluate", async (Guid id, EvaluateRequest req, ISender sender) =>
            Results.Ok(await sender.Send(new EvaluateDistributionCommand(id, req.Score, req.Comment))));
    }

    private record EvaluateRequest(int Score, string? Comment);
}
