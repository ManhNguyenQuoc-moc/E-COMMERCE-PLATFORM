using FluentValidation;

namespace EcommercePlatform.Application.Features.Orders.Commands.Checkout;

public sealed class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator()
    {
        RuleFor(x => x.ShippingRecipientName)
            .NotEmpty().WithMessage("Shipping recipient name is required.")
            .MaximumLength(150).WithMessage("Shipping recipient name must not exceed 150 characters.");

        RuleFor(x => x.ShippingPhone)
            .NotEmpty().WithMessage("Shipping phone is required.")
            .MaximumLength(20).WithMessage("Shipping phone must not exceed 20 characters.");

        RuleFor(x => x.ShippingAddress)
            .NotEmpty().WithMessage("Shipping address is required.")
            .MaximumLength(500).WithMessage("Shipping address must not exceed 500 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(x => x.Notes is not null);
    }
}
