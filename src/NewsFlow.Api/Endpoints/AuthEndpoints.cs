using MediatR;
using NewsFlow.Application.Auth.Commands;
using NewsFlow.Application.Auth.Queries;

namespace NewsFlow.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginCommand command, ISender sender) =>
        {
            try
            {
                var result = await sender.Send(command);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        });

        group.MapPost("/refresh", async (RefreshTokenCommand command, ISender sender) =>
        {
            try
            {
                var result = await sender.Send(command);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        });

        group.MapGet("/me", async (ISender sender) =>
        {
            try
            {
                var result = await sender.Send(new GetCurrentUserQuery());
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        }).RequireAuthorization();

        group.MapPost("/logout", () => Results.Ok(new { message = "Logged out" }))
            .RequireAuthorization();
    }
}
