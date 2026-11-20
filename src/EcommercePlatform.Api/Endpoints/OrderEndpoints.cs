using EcommercePlatform.Application.Common.Models;
using EcommercePlatform.Application.Features.Orders.Commands.Checkout;
using EcommercePlatform.Application.Features.Orders.DTOs;
using EcommercePlatform.Application.Features.Orders.Queries.GetOrders;
using MediatR;

namespace EcommercePlatform.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/orders")
            .WithTags("Orders")
            .RequireAuthorization("UserOrAdmin");

        group.MapPost("/checkout", async (
                CheckoutCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var order = await sender.Send(command, cancellationToken);
                return TypedResults.Created($"/api/v1/orders/{order.Id}", order);
            })
            .WithName("Checkout")
            .RequireRateLimiting("CartWritePolicy")
            .Produces<OrderDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapGet("/", async (
                ISender sender,
                CancellationToken cancellationToken,
                int page = 1,
                int size = 10) =>
            {
                var orders = await sender.Send(new GetOrdersQuery(page, size), cancellationToken);
                return TypedResults.Ok(orders);
            })
            .WithName("GetOrders")
            .Produces<PaginatedList<OrderDto>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}
