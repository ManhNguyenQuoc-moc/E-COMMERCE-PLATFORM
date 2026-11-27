using System.Collections.Concurrent;
using System.Text.Json;
using EcommercePlatform.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace EcommercePlatform.Infrastructure.Services;

public sealed class DistributedCacheService(
    IDistributedCache cache,
    ILogger<DistributedCacheService> logger) : ICacheService
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    // 1. Tạo một "Tủ treo chìa khóa" dùng chung cho toàn bộ ứng dụng (static):
    // - Kiểu ConcurrentDictionary<string, SemaphoreSlim>: An toàn khi nhiều luồng (threads) truy cập cùng lúc.
    // - Key (string): Tên của cacheKey (ví dụ: "products:by-id:101" hoặc "products:list:v2:p1...").
    // - Value (SemaphoreSlim): Ổ khóa dành RIÊNG cho đúng cái cacheKey đó.
    // -> Tại sao không dùng 1 ổ khóa chung mà phải chia theo từng key?
    //    Vì nếu khách A đang bị Cache Miss ở "Sản phẩm 101", ta chỉ muốn những ai cũng hỏi "Sản phẩm 101" đứng chờ thôi;
    //    còn khách B đang hỏi "Sản phẩm 202" (hoặc Trang 2) thì vẫn chạy song song bình thường, không bị vạ lây!
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> KeyLocks = new();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await cache.GetStringAsync(key, cancellationToken);
            return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis GET failed for key {CacheKey}. Falling back to database.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var ttl = expiration ?? DefaultExpiration;
            var jitter = TimeSpan.FromSeconds(Random.Shared.Next(0, 15)); // Chống Cache Avalanche
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl + jitter
            };

            var json = JsonSerializer.Serialize(value);
            await cache.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis SET failed for key {CacheKey}.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis REMOVE failed for key {CacheKey}.", key);
        }
    }

    public async Task<string> GetVersionAsync(string namespaceKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var version = await cache.GetStringAsync(namespaceKey, cancellationToken);
            if (!string.IsNullOrEmpty(version))
            {
                return version;
            }

            const string initialVersion = "1";
            await cache.SetStringAsync(namespaceKey, initialVersion, cancellationToken);
            return initialVersion;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis GetVersion failed for {NamespaceKey}.", namespaceKey);
            return "1";
        }
    }

    public async Task IncrementVersionAsync(string namespaceKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var newVersion = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            await cache.SetStringAsync(namespaceKey, newVersion, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis IncrementVersion failed for {NamespaceKey}.", namespaceKey);
        }
    }

    // 2. Hàm tổng quát GetOrCreateAsync<T> (Chống Cache Stampede khi nhiều người cùng bị Cache Miss 1 lúc):
    // - key: Tên key trong Redis cần lấy.
    // - factory: Một hàm callback (delegate) chứa câu lệnh query SQL Server.
    //            Lưu ý: Truyền vào là một HÀM (Func) chứ chưa chạy ngay! Chỉ khi nào thật sự cần mới gọi factory() để chạy SQL.
    // - expiration: Thời gian sống (TTL) của key trên Redis.
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        // =========================================================================
        // BƯỚC 1: KIỂM TRA LẦN 1 (NGOÀI CỬA KHÓA - FAST PATH)
        // =========================================================================
        // Hỏi Redis xem đã có dữ liệu của key này chưa?
        var cached = await GetAsync<T>(key, cancellationToken);

        // Nếu ĐÃ CÓ (Cache Hit - chiếm 99.9% các request thông thường):
        if (cached is not null)
        {
            // Trả về ngay lập tức trong ~1ms!
            // Không cần đụng tới ổ khóa SemaphoreSlim ở dưới -> Tốc độ cực đại.
            return cached;
        }

        // =========================================================================
        // BƯỚC 2: XẾP HÀNG QUA CỔNG KHÓA (KHI XẢY RA CACHE MISS)
        // =========================================================================
        // Nếu chạy xuống tới dòng này nghĩa là: Trong Redis CHƯA CÓ dữ liệu (Cache Miss).
        // Giả sử đúng mili-giây này có 100 request cùng hỏi chung 1 key và cùng nhảy xuống đây:

        // Tìm trong "Tủ chìa khóa" xem key này đã có ổ khóa nào chưa:
        // - Nếu chưa có: Tạo mới new SemaphoreSlim(1, 1) -> Số (1, 1) nghĩa là "Cửa xoay chỉ cho tối đa 1 người đi vào tại 1 thời điểm".
        // - Nếu đã có rồi: Lấy lại đúng ổ khóa đó ra để cả 100 request cùng xếp hàng vào đúng 1 cửa này.
        var keyLock = KeyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        // Cả 100 request cùng gọi WaitAsync() để xin đi qua cửa:
        // - Request #1 (đến sớm nhất 1 micro-giây): Đi qua cửa ngay lập tức, đồng thời cửa tự động KHÓA LẠI (count giảm từ 1 về 0).
        // - Request #2 đến #100: Thấy cửa đã khóa nên tạm dừng (await bất đồng bộ, không làm treo CPU) đứng xếp hàng chờ ở dòng này.
        await keyLock.WaitAsync(cancellationToken);

        try
        {
            // =====================================================================
            // BƯỚC 3: KIỂM TRA LẦN 2 (BÊN TRONG CỬA KHÓA - DOUBLE CHECK)
            // =====================================================================
            // Tại sao vừa kiểm tra Redis ở Bước 1 rồi mà vào đây lại phải hỏi Redis lần nữa?
            // - Với Request #1 (người đầu tiên vào cửa): Hỏi lại Redis thì tất nhiên vẫn chưa có (cached == null) -> Đi tiếp xuống Bước 4.
            // - NHƯNG với Request #2 -> #100 (những người đứng chờ ở cửa):
            //   Khi họ được mở cửa bước vào đây, tức là Request #1 ĐÃ chạy xong Bước 4 (đã lấy từ SQL và lưu vào Redis xong xuôi rồi)!
            cached = await GetAsync<T>(key, cancellationToken);

            // Vì vậy, khi Request #2 -> #100 hỏi lại Redis ở dòng trên, cached sẽ KHÁC NULL!
            if (cached is not null)
            {
                // Request #2 -> #100 lấy luôn dữ liệu từ Redis mà Request #1 vừa cất vào và thoát ra!
                // -> 99 request này KHÔNG BAO GIỜ chạm xuống Bước 4 (không gọi SQL Server).
                return cached;
            }

            // =====================================================================
            // BƯỚC 4: CHỈ ĐÚNG 1 REQUEST ĐẦU TIÊN CHẠY XUỐNG SQL SERVER
            // =====================================================================
            // Chỉ có duy nhất Request #1 (khi cả Bước 1 lẫn Bước 3 đều null) mới lọt xuống được dòng này.
            // Bây giờ mới chính thức kích hoạt hàm factory() để chạy câu lệnh SELECT xuống SQL Server:
            var freshData = await factory(cancellationToken);

            // Nếu SQL Server trả về dữ liệu hợp lệ:
            if (freshData is not null)
            {
                // Lưu dữ liệu mới lấy từ SQL Server lên kệ Redis (kèm TTL + Jitter).
                // Ngay khoảnh khắc dòng này chạy xong, trên Redis đã có sẵn dữ liệu cho Request #2 -> #100!
                await SetAsync(key, freshData, expiration, cancellationToken);
            }

            // Trả dữ liệu về cho Request #1.
            return freshData;
        }
        finally
        {
            // =====================================================================
            // BƯỚC 5: MỞ CỬA KHÓA CHO NGƯỜI TIẾP THEO (LUÔN CHẠY DÙ CÓ LỖI HAY KHÔNG)
            // =====================================================================
            // Khối finally đảm bảo: Dù ở Bước 4 SQL Server chạy thành công hay bị văng Exception (ví dụ 404 NotFoundException),
            // thì khi đi ra BẮT BUỘC phải gọi Release() để mở chốt cửa (tăng count từ 0 lên 1).
            // Nhờ đó Request #2 đang chờ ở WaitAsync() mới được bước vào Bước 3, rồi lần lượt #3, #4... đi qua và lấy dữ liệu từ Redis!
            keyLock.Release();
        }
    }
}