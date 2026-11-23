using EcommercePlatform.Application.Features.Coupons.Commands.CreateCoupon;
using EcommercePlatform.Application.Features.Coupons.DTOs;
using EcommercePlatform.Application.Features.Orders.Commands.UpdateOrderStatus;
using EcommercePlatform.Application.Features.Orders.DTOs;
using MediatR;

namespace EcommercePlatform.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin")
            .WithTags("Admin")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/coupons", async (
                CreateCouponCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var coupon = await sender.Send(command, cancellationToken);
                return TypedResults.Created($"/api/v1/admin/coupons/{coupon.Id}", coupon);
            })
            .WithName("CreateCoupon")
            .Produces<CouponDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/orders/{id:guid}/status", async (
                Guid id,
                UpdateOrderStatusRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var order = await sender.Send(new UpdateOrderStatusCommand(id, request.Status), cancellationToken);
                return TypedResults.Ok(order);
            })
            .WithName("UpdateOrderStatus")
            .Produces<OrderDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}

public sealed record UpdateOrderStatusRequest(string Status);
