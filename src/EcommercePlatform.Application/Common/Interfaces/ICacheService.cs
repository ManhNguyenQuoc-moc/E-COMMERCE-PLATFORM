namespace EcommercePlatform.Application.Common.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<string> GetVersionAsync(string namespaceKey, CancellationToken cancellationToken = default);
    Task IncrementVersionAsync(string namespaceKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy dữ liệu từ Redis Cache. Nếu Cache Miss, sử dụng cơ chế khóa Single-Flight (Double-Checked Locking)
    /// để đảm bảo tại 1 thời điểm chỉ có duy nhất 1 request gọi xuống SQL Server (factory),
    /// các request đồng thời còn lại sẽ chờ và lấy chung kết quả từ Redis.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);
}