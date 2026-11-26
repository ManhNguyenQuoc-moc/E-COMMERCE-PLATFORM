namespace EcommercePlatform.Application.Features.Cart.DTOs;

public sealed record CartItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    int AvailableStock,
    bool IsAvailable = true);

public sealed record CartDto(
    Guid Id,
    Guid UserId,
    Guid? AppliedCouponId,
    IReadOnlyCollection<CartItemDto> Items,
    int TotalItems,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TotalAmount)
{
    public static CartDto Empty(Guid userId) =>
        new(Guid.Empty, userId, null, [], 0, 0m, 0m, 0m);
}
