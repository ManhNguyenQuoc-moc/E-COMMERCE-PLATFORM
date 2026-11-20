namespace EcommercePlatform.Application.Features.Orders.DTOs;

public sealed record OrderDto(
    Guid Id,
    Guid UserId,
    Guid? CouponId,
    string OrderNumber,
    string Status,
    decimal SubTotal,
    decimal CouponDiscountAmount,
    decimal RankDiscountAmount,
    decimal TotalAmount,
    string ShippingRecipientName,
    string ShippingPhone,
    string ShippingAddress,
    string? Notes,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<OrderItemDto> Items);
