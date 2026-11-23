using FluentValidation;

namespace EcommercePlatform.Application.Features.Reviews.Queries.GetProductReviews;

public sealed class GetProductReviewsValidator : AbstractValidator<GetProductReviewsQuery>
{
    public GetProductReviewsValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.Size)
            .InclusiveBetween(1, 100).WithMessage("Size must be between 1 and 100.");
    }
}
