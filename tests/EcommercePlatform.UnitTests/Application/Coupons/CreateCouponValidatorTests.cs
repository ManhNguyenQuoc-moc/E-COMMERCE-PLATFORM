using EcommercePlatform.Application.Features.Coupons.Commands.CreateCoupon;

namespace EcommercePlatform.UnitTests.Application.Coupons;

public sealed class CreateCouponValidatorTests
{
    private readonly CreateCouponValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        var command = new CreateCouponCommand(
            "SALE20",
            "PERCENTAGE",
            20m,
            100_000m,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7),
            100);

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_Fail_When_PercentageExceeds100_Or_EndDateInvalid()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new CreateCouponCommand("BAD", "PERCENTAGE", 120m, -1m, now, now.AddDays(-1), 0);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCouponCommand.Value));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCouponCommand.MinOrderValue));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCouponCommand.EndDate));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCouponCommand.UsageLimit));
    }
}
