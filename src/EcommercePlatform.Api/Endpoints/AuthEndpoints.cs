using EcommercePlatform.Application.Features.Auth.Commands.Login;
using EcommercePlatform.Application.Features.Auth.Commands.Register;
using EcommercePlatform.Application.Features.Auth.DTOs;
using MediatR;

namespace EcommercePlatform.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Authentication")
            .RequireRateLimiting("AuthPolicy");

        group.MapPost("/register", async (
                RegisterCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var response = await sender.Send(command, cancellationToken);
                return TypedResults.Created($"/api/v1/users/{response.UserId}", response);
            })
            .WithName("Register")
            .AllowAnonymous()
            .Produces<AuthResponseDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/login", async (
                LoginCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var response = await sender.Send(command, cancellationToken);
                return TypedResults.Ok(response);
            })
            .WithName("Login")
            .AllowAnonymous()
            .Produces<AuthResponseDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return app;
    }
}
