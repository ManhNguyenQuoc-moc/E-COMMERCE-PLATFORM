using EcommercePlatform.Application.Features.Cart.Commands.ApplyCoupon;

namespace EcommercePlatform.UnitTests.Application.Cart;

public sealed class ApplyCouponValidatorTests
{
    private readonly ApplyCouponValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_CodeIsValid()
    {
        var result = _validator.Validate(new ApplyCouponCommand("SALE10"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_CodeIsEmpty(string code)
    {
        var result = _validator.Validate(new ApplyCouponCommand(code));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ApplyCouponCommand.Code));
    }

    [Fact]
    public void Validate_Should_Fail_When_CodeExceeds50Characters()
    {
        var result = _validator.Validate(new ApplyCouponCommand(new string('A', 51)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ApplyCouponCommand.Code));
    }
}
