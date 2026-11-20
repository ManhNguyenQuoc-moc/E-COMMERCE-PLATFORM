using EcommercePlatform.Application.Features.Cart.Commands.AddToCart;

namespace EcommercePlatform.UnitTests.Application.Cart;

public sealed class AddToCartValidatorTests
{
    private readonly AddToCartValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        var result = _validator.Validate(new AddToCartCommand(Guid.NewGuid(), 2));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_Fail_When_ProductIdEmptyOrQuantityNotPositive()
    {
        Assert.False(_validator.Validate(new AddToCartCommand(Guid.Empty, 1)).IsValid);
        Assert.False(_validator.Validate(new AddToCartCommand(Guid.NewGuid(), 0)).IsValid);
        Assert.False(_validator.Validate(new AddToCartCommand(Guid.NewGuid(), -2)).IsValid);
    }
}
