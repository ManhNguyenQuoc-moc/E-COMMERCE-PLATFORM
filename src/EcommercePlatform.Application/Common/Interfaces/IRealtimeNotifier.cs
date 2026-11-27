namespace EcommercePlatform.Application.Common.Interfaces;

public sealed record RealtimeNotificationMessage(
    string Title,
    string Message,
    string Category,
    string Level,
    DateTimeOffset CreatedAt);

public interface IRealtimeNotifier
{
    /// <summary>
    /// Bắn thông báo real-time tới tất cả Admin (hoặc toàn bộ màn hình Dashboard đang mở)
    /// </summary>
    Task BroadcastToDashboardAsync(
        RealtimeNotificationMessage notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bắn thông báo real-time tới riêng 1 User cụ thể (ví dụ khi đơn hàng của họ đổi trạng thái)
    /// </summary>
    Task SendToUserAsync(
        Guid userId,
        RealtimeNotificationMessage notification,
        CancellationToken cancellationToken = default);
}