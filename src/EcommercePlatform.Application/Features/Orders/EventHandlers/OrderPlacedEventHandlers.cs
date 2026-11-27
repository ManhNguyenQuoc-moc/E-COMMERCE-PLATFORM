using EcommercePlatform.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EcommercePlatform.Application.Features.Orders.EventHandlers;

/// <summary>
/// HANDLER 1: Lắng nghe sự kiện Đặt hàng thành công để tự động làm mới Redis Cache sản phẩm (vì tồn kho vừa bị trừ)
/// </summary>
public sealed class OrderPlacedCacheInvalidationHandler(
    ICacheService cacheService,
    ILogger<OrderPlacedCacheInvalidationHandler> logger)
    : INotificationHandler<OrderPlacedNotification>
{
    public async Task Handle(OrderPlacedNotification notification, CancellationToken cancellationToken)
    {
        foreach (var productId in notification.PurchasedProductIds)
        {
            await cacheService.RemoveAsync($"products:by-id:{productId}", cancellationToken);
        }

        await cacheService.IncrementVersionAsync("products:version", cancellationToken);

        logger.LogInformation(
            "Invalidated Redis product cache after Order {OrderNumber} was placed.",
            notification.OrderNumber);
    }
}

/// <summary>
/// HANDLER 2: Lắng nghe sự kiện Đặt hàng thành công để bắn thông báo Real-time qua SignalR ra màn hình Frontend
/// </summary>
public sealed class OrderPlacedRealtimeHandler(
    IRealtimeNotifier realtimeNotifier,
    ILogger<OrderPlacedRealtimeHandler> logger)
    : INotificationHandler<OrderPlacedNotification>
{
    public async Task Handle(OrderPlacedNotification notification, CancellationToken cancellationToken)
    {
        var message = new RealtimeNotificationMessage(
            Title: $"Đơn hàng mới #{notification.OrderNumber}",
            Message: $"Khách hàng {notification.RecipientName} vừa đặt đơn hàng trị giá {notification.TotalAmount:N0} ₫.",
            Category: "order",
            Level: "success",
            CreatedAt: DateTimeOffset.UtcNow);

        await realtimeNotifier.BroadcastToDashboardAsync(message, cancellationToken);

        logger.LogInformation(
            "Broadcasted SignalR notification for Order {OrderNumber}.",
            notification.OrderNumber);
    }
}