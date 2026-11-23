using EcommercePlatform.Application.Features.Reviews.Queries.GetProductReviews;

namespace EcommercePlatform.UnitTests.Application.Reviews;

public sealed class GetProductReviewsValidatorTests
{
    private readonly GetProductReviewsValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_QueryIsValid()
    {
        var result = _validator.Validate(new GetProductReviewsQuery(Guid.NewGuid(), 1, 10));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_Fail_When_ProductIdEmptyOrPaginationInvalid()
    {
        var result = _validator.Validate(new GetProductReviewsQuery(Guid.Empty, 0, 101));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetProductReviewsQuery.ProductId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetProductReviewsQuery.Page));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetProductReviewsQuery.Size));
    }
}
