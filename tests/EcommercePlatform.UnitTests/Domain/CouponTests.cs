using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.UnitTests.Domain;

public sealed class CouponTests
{
    [Fact]
    public void Constructor_Should_NormalizeCodeAndSetDefaults()
    {
        var start = DateTimeOffset.UtcNow.AddDays(-1);
        var end = DateTimeOffset.UtcNow.AddDays(5);

        var coupon = new Coupon("  sale10  ", DiscountType.Percentage, 10m, 100_000m, start, end, 50);

        Assert.Equal("SALE10", coupon.Code);
        Assert.Equal(0, coupon.UsedCount);
        Assert.True(coupon.IsActive);
    }

    [Fact]
    public void Constructor_Should_ThrowDomainException_When_InvalidParameters()
    {
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(1);

        Assert.Throws<DomainException>(() => new Coupon("", DiscountType.FixedAmount, 10_000m, 0m, start, end, 10));
        Assert.Throws<DomainException>(() => new Coupon("CODE", DiscountType.Percentage, 101m, 0m, start, end, 10));
        Assert.Throws<DomainException>(() => new Coupon("CODE", DiscountType.FixedAmount, 0m, 0m, start, end, 10));
        Assert.Throws<DomainException>(() => new Coupon("CODE", DiscountType.FixedAmount, 10_000m, 0m, end, start, 10));
        Assert.Throws<DomainException>(() => new Coupon("CODE", DiscountType.FixedAmount, 10_000m, 0m, start, end, 0));
    }

    [Fact]
    public void ValidateCanApply_Should_ThrowDomainException_When_ExpiredOrBelowMinOrderOrLimitReached()
    {
        var now = DateTimeOffset.UtcNow;
        var expired = new Coupon("OLD", DiscountType.FixedAmount, 20_000m, 100_000m, now.AddDays(-5), now.AddDays(-1), 5);
        Assert.Throws<DomainException>(() => expired.ValidateCanApply(200_000m, now));

        var active = new Coupon("NEW", DiscountType.FixedAmount, 20_000m, 100_000m, now.AddDays(-1), now.AddDays(5), 1);
        Assert.Throws<DomainException>(() => active.ValidateCanApply(50_000m, now));

        active.IncrementUsage();
        Assert.Throws<DomainException>(() => active.ValidateCanApply(200_000m, now));
    }

    [Fact]
    public void CalculateDiscount_Should_ComputePercentageAndFixedAmountCappedAtSubTotal()
    {
        var start = DateTimeOffset.UtcNow.AddDays(-1);
        var end = DateTimeOffset.UtcNow.AddDays(5);

        var percentCoupon = new Coupon("P15", DiscountType.Percentage, 15m, 0m, start, end, 10);
        Assert.Equal(30_000m, percentCoupon.CalculateDiscount(200_000m));

        var fixedCoupon = new Coupon("F100", DiscountType.FixedAmount, 100_000m, 0m, start, end, 10);
        Assert.Equal(60_000m, fixedCoupon.CalculateDiscount(60_000m));
    }
}
