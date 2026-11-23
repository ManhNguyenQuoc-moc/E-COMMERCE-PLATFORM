using EcommercePlatform.Domain.Enums;
using FluentValidation;

namespace EcommercePlatform.Application.Features.Orders.Commands.UpdateOrderStatus;

public sealed class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    public UpdateOrderStatusValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("OrderId is required.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Order status is required.")
            .Must(v => TryParseOrderStatus(v, out _))
            .WithMessage("Invalid order status. Allowed values: Pending, Confirmed, Processing, Shipped, Completed, Cancelled.");
    }

    public static bool TryParseOrderStatus(string? value, out OrderStatus status)
    {
        status = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return Enum.TryParse(value.Trim(), ignoreCase: true, out status)
               && Enum.IsDefined(status);
    }
}
