using MediatR;

namespace EcommercePlatform.Application.Features.Orders.EventHandlers;

public sealed record OrderPlacedNotification(
    Guid OrderId,
    Guid UserId,
    string OrderNumber,
    decimal TotalAmount,
    string RecipientName,
    IReadOnlyCollection<Guid> PurchasedProductIds) : INotification;