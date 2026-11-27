using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace EcommercePlatform.Api.Hubs;

public sealed class NotificationHub : Hub
{
    public const string DashboardGroup = "DashboardClients";

    public override async Task OnConnectedAsync()
    {
        // Mọi client mở trang Dashboard đều được thêm vào nhóm nhận thông báo trực tiếp
        await Groups.AddToGroupAsync(Context.ConnectionId, DashboardGroup);

        // Nếu user có truyền JWT Token, thêm luôn vào nhóm riêng của UserId đó
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        await base.OnConnectedAsync();
    }
}