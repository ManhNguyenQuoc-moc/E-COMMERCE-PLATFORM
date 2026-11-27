using EcommercePlatform.Api.Hubs;
using EcommercePlatform.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace EcommercePlatform.Api.Services;

public sealed class SignalRRealtimeNotifier(
    IHubContext<NotificationHub> hubContext) : IRealtimeNotifier
{
    public async Task BroadcastToDashboardAsync(
        RealtimeNotificationMessage notification,
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients
            .Group(NotificationHub.DashboardGroup)
            .SendAsync("ReceiveNotification", notification, cancellationToken);
    }

    public async Task SendToUserAsync(
        Guid userId,
        RealtimeNotificationMessage notification,
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients
            .Group($"user:{userId}")
            .SendAsync("ReceiveNotification", notification, cancellationToken);
    }
}