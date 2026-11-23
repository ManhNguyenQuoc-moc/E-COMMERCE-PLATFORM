using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.UnitTests.Domain;

public sealed class ReviewTests
{
    [Fact]
    public void Constructor_Should_CreateReviewAndTrimComment_When_Valid()
    {
        var productId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var review = new Review(productId, userId, orderId, 5, "  Great serum!  ");

        Assert.Equal(productId, review.ProductId);
        Assert.Equal(userId, review.UserId);
        Assert.Equal(orderId, review.OrderId);
        Assert.Equal(5, review.Rating);
        Assert.Equal("Great serum!", review.Comment);
    }

    [Fact]
    public void Constructor_Should_SetCommentNull_When_Whitespace()
    {
        var review = new Review(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 4, "   ");

        Assert.Null(review.Comment);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Constructor_Should_ThrowDomainException_When_RatingOutOfRange(int invalidRating)
    {
        Assert.Throws<DomainException>(() =>
            new Review(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), invalidRating, "Ok"));
    }

    [Fact]
    public void Constructor_Should_ThrowDomainException_When_IdsEmptyOrCommentTooLong()
    {
        Assert.Throws<DomainException>(() => new Review(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), 5));
        Assert.Throws<DomainException>(() => new Review(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), 5));
        Assert.Throws<DomainException>(() => new Review(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 5));
        Assert.Throws<DomainException>(() => new Review(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5, new string('A', 2001)));
    }
}
