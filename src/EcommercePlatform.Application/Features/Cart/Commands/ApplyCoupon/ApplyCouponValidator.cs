using FluentValidation;

namespace EcommercePlatform.Application.Features.Cart.Commands.ApplyCoupon;

public sealed class ApplyCouponValidator : AbstractValidator<ApplyCouponCommand>
{
    public ApplyCouponValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Coupon code is required.")
            .MaximumLength(50).WithMessage("Coupon code must not exceed 50 characters.");
    }
}
