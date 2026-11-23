namespace EcommercePlatform.Application.Features.Coupons.DTOs;

public sealed record CouponDto(
    Guid Id,
    string Code,
    string DiscountType,
    decimal Value,
    decimal MinOrderValue,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    int UsageLimit,
    int UsedCount,
    bool IsActive,
    DateTimeOffset CreatedAt);
