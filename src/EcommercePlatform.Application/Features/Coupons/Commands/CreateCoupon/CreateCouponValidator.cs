using EcommercePlatform.Domain.Enums;
using FluentValidation;

namespace EcommercePlatform.Application.Features.Coupons.Commands.CreateCoupon;

public sealed class CreateCouponValidator : AbstractValidator<CreateCouponCommand>
{
    public CreateCouponValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Coupon code is required.")
            .MaximumLength(50).WithMessage("Coupon code must not exceed 50 characters.");

        RuleFor(x => x.DiscountType)
            .NotEmpty().WithMessage("Discount type is required.")
            .Must(v => TryParseDiscountType(v, out _))
            .WithMessage("Discount type must be 'PERCENTAGE' or 'FIXED_AMOUNT'.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("Coupon value must be greater than zero.");

        RuleFor(x => x.Value)
            .LessThanOrEqualTo(100).WithMessage("Percentage discount value cannot exceed 100.")
            .When(x => TryParseDiscountType(x.DiscountType, out var dt) && dt == DiscountType.Percentage);

        RuleFor(x => x.MinOrderValue)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum order value cannot be negative.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");

        RuleFor(x => x.UsageLimit)
            .GreaterThan(0).WithMessage("Usage limit must be greater than zero.");
    }

    public static bool TryParseDiscountType(string? value, out DiscountType discountType)
    {
        discountType = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Trim().Replace("_", string.Empty, StringComparison.Ordinal);
        return Enum.TryParse(normalized, ignoreCase: true, out discountType)
               && Enum.IsDefined(discountType);
    }
}
