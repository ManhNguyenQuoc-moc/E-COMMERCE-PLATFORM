using EcommercePlatform.Application.Features.Cart.Commands.AddToCart;
using EcommercePlatform.Application.Features.Cart.Commands.UpdateCartItem;
using EcommercePlatform.Application.Features.Cart.DTOs;
using EcommercePlatform.Application.Features.Cart.Queries.GetCart;
using MediatR;

namespace EcommercePlatform.Api.Endpoints;

public static class CartEndpoints
{
    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/cart")
            .WithTags("Cart")
            .RequireAuthorization("UserOrAdmin");

        group.MapGet("/", async (ISender sender, CancellationToken cancellationToken) =>
            {
                var cart = await sender.Send(new GetCartQuery(), cancellationToken);
                return TypedResults.Ok(cart);
            })
            .WithName("GetCart")
            .Produces<CartDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/add", async (AddToCartCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                var cart = await sender.Send(command, cancellationToken);
                return TypedResults.Ok(cart);
            })
            .WithName("AddToCart")
            .RequireRateLimiting("CartWritePolicy")
            .Produces<CartDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPut("/items/{id:guid}", async (
                Guid id,
                UpdateCartItemRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var cart = await sender.Send(new UpdateCartItemCommand(id, request.Quantity), cancellationToken);
                return TypedResults.Ok(cart);
            })
            .WithName("UpdateCartItem")
            .RequireRateLimiting("CartWritePolicy")
            .Produces<CartDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return app;
    }
}

public sealed record UpdateCartItemRequest(int Quantity);
