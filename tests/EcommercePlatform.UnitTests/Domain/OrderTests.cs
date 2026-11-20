using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.UnitTests.Domain;

public sealed class OrderTests
{
    [Fact]
    public void Constructor_Should_ThrowDomainException_When_RequiredFieldsAreInvalid()
    {
        var userId = Guid.NewGuid();

        Assert.Throws<DomainException>(() => new Order(Guid.Empty, "ORD-01", "Alice", "0901234567", "123 Street"));
        Assert.Throws<DomainException>(() => new Order(userId, "", "Alice", "0901234567", "123 Street"));
        Assert.Throws<DomainException>(() => new Order(userId, "ORD-01", "", "0901234567", "123 Street"));
        Assert.Throws<DomainException>(() => new Order(userId, "ORD-01", "Alice", " ", "123 Street"));
        Assert.Throws<DomainException>(() => new Order(userId, "ORD-01", "Alice", "0901234567", ""));
    }

    [Fact]
    public void AddItem_And_ApplyDiscounts_Should_CalculateTotalsCorrectly()
    {
        var order = new Order(Guid.NewGuid(), "ORD-01", "Alice", "0901234567", "123 Street", "Leave at door");
        order.AddItem(Guid.NewGuid(), "Serum", "SER-01", 250_000m, 2);
        order.AddItem(Guid.NewGuid(), "Cleanser", "CLN-01", 100_000m, 1);

        order.ApplyDiscounts(UserRank.Gold, couponDiscountAmount: 50_000m);

        Assert.Equal(600_000m, order.SubTotal);
        Assert.Equal(30_000m, order.RankDiscountAmount); // 5% of 600,000
        Assert.Equal(50_000m, order.CouponDiscountAmount);
        Assert.Equal(520_000m, order.TotalAmount);
    }

    [Fact]
    public void ApplyDiscounts_Should_ThrowDomainException_When_CouponDiscountIsNegative()
    {
        var order = new Order(Guid.NewGuid(), "ORD-01", "Alice", "0901234567", "123 Street");
        order.AddItem(Guid.NewGuid(), "Serum", "SER-01", 100_000m, 1);

        Assert.Throws<DomainException>(() => order.ApplyDiscounts(UserRank.Member, -1m));
    }

    [Fact]
    public void ApplyDiscounts_Should_ClampCouponDiscount_When_ExceedingRemainingAmount()
    {
        var order = new Order(Guid.NewGuid(), "ORD-01", "Alice", "0901234567", "123 Street");
        order.AddItem(Guid.NewGuid(), "Serum", "SER-01", 100_000m, 1);

        order.ApplyDiscounts(UserRank.Diamond, couponDiscountAmount: 150_000m); // 10% rank = 10,000; remaining = 90,000

        Assert.Equal(10_000m, order.RankDiscountAmount);
        Assert.Equal(90_000m, order.CouponDiscountAmount);
        Assert.Equal(0m, order.TotalAmount);
    }
}
