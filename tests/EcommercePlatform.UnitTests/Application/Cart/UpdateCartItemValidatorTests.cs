using EcommercePlatform.Application.Features.Cart.Commands.UpdateCartItem;

namespace EcommercePlatform.UnitTests.Application.Cart;

public sealed class UpdateCartItemValidatorTests
{
    private readonly UpdateCartItemValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_QuantityIsZeroOrPositive()
    {
        Assert.True(_validator.Validate(new UpdateCartItemCommand(Guid.NewGuid(), 0)).IsValid);
        Assert.True(_validator.Validate(new UpdateCartItemCommand(Guid.NewGuid(), 3)).IsValid);
    }

    [Fact]
    public void Validate_Should_Fail_When_CartItemIdEmptyOrQuantityNegative()
    {
        Assert.False(_validator.Validate(new UpdateCartItemCommand(Guid.Empty, 1)).IsValid);
        Assert.False(_validator.Validate(new UpdateCartItemCommand(Guid.NewGuid(), -1)).IsValid);
    }
}
