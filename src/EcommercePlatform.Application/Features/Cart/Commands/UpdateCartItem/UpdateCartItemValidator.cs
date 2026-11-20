using FluentValidation;

namespace EcommercePlatform.Application.Features.Cart.Commands.UpdateCartItem;

public sealed class UpdateCartItemValidator : AbstractValidator<UpdateCartItemCommand>
{
    public UpdateCartItemValidator()
    {
        RuleFor(x => x.CartItemId)
            .NotEmpty().WithMessage("CartItemId is required.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity must be greater than or equal to zero.");
    }
}
