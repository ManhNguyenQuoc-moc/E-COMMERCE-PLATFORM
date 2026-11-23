using EcommercePlatform.Application.Features.Reviews.Commands.CreateReview;

namespace EcommercePlatform.UnitTests.Application.Reviews;

public sealed class CreateReviewValidatorTests
{
    private readonly CreateReviewValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        var result = _validator.Validate(new CreateReviewCommand(Guid.NewGuid(), 5, "Excellent product"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_Fail_When_ProductIdEmptyOrRatingInvalidOrCommentTooLong()
    {
        var result = _validator.Validate(new CreateReviewCommand(Guid.Empty, 6, new string('x', 2001)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReviewCommand.ProductId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReviewCommand.Rating));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReviewCommand.Comment));
    }
}
