using MediatR;
using NewsFlow.Application.Users.Commands;
using NewsFlow.Application.Users.DTOs;
using NewsFlow.Application.Users.Queries;

namespace NewsFlow.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/users")
            .WithTags("Users")
            .RequireAuthorization("AdminPolicy");

        group.MapGet("/", async (int? page, int? pageSize, string? search, ISender sender) =>
        {
            var result = await sender.Send(new GetUsersQuery(page ?? 1, pageSize ?? 20, search));
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            try
            {
                var result = await sender.Send(new GetUserByIdQuery(id));
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        });

        group.MapPost("/", async (CreateUserDto dto, ISender sender) =>
        {
            try
            {
                var result = await sender.Send(new CreateUserCommand(dto));
                return Results.Created($"/api/admin/users/{result.Id}", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateUserDto dto, ISender sender) =>
        {
            try
            {
                var result = await sender.Send(new UpdateUserCommand(id, dto));
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            try
            {
                await sender.Send(new DeleteUserCommand(id));
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        });
    }
}
