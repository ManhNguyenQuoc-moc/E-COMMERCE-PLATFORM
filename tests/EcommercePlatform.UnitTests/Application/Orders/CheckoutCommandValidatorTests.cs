using EcommercePlatform.Application.Features.Orders.Commands.Checkout;

namespace EcommercePlatform.UnitTests.Application.Orders;

public sealed class CheckoutCommandValidatorTests
{
    private readonly CheckoutCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        var command = new CheckoutCommand(
            "Nguyen Van A",
            "0901234567",
            "123 Le Loi, District 1, HCMC",
            "Call before delivery");

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_Fail_When_RequiredShippingFieldsAreEmpty()
    {
        var command = new CheckoutCommand("", "", "");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CheckoutCommand.ShippingRecipientName));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CheckoutCommand.ShippingPhone));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CheckoutCommand.ShippingAddress));
    }
}
