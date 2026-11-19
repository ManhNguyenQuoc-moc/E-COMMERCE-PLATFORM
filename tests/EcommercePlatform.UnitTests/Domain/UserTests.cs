using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.UnitTests.Domain;

public sealed class UserTests
{
    [Fact]
    public void Constructor_Should_InitializeDefaultRankAndZeroPoints_When_InputsAreValid()
    {
        var accountId = Guid.NewGuid();
        var user = new User(accountId, "  Alice Smith ", " 0901234567 ", " 123 Main St ");

        Assert.Equal(accountId, user.AccountId);
        Assert.Equal("Alice Smith", user.FullName);
        Assert.Equal("0901234567", user.PhoneNumber);
        Assert.Equal("123 Main St", user.DefaultShippingAddress);
        Assert.Equal(0, user.LoyaltyPoints);
        Assert.Equal(UserRank.Member, user.Rank);
    }

    [Fact]
    public void Constructor_Should_ThrowDomainException_When_AccountIdIsEmpty()
    {
        Assert.Throws<DomainException>(() => new User(Guid.Empty, "Alice Smith"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Should_ThrowDomainException_When_FullNameIsEmpty(string fullName)
    {
        Assert.Throws<DomainException>(() => new User(Guid.NewGuid(), fullName));
    }

    [Theory]
    [InlineData(50, UserRank.Member)]
    [InlineData(100, UserRank.Silver)]
    [InlineData(500, UserRank.Gold)]
    [InlineData(1000, UserRank.Diamond)]
    public void AddLoyaltyPoints_Should_UpgradeRankCorrectly_When_PointsReachThreshold(
        int pointsToAdd,
        UserRank expectedRank)
    {
        var user = new User(Guid.NewGuid(), "Alice Smith");

        user.AddLoyaltyPoints(pointsToAdd);

        Assert.Equal(pointsToAdd, user.LoyaltyPoints);
        Assert.Equal(expectedRank, user.Rank);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void AddLoyaltyPoints_Should_ThrowDomainException_When_PointsAreNotPositive(int invalidPoints)
    {
        var user = new User(Guid.NewGuid(), "Alice Smith");

        Assert.Throws<DomainException>(() => user.AddLoyaltyPoints(invalidPoints));
    }
}
