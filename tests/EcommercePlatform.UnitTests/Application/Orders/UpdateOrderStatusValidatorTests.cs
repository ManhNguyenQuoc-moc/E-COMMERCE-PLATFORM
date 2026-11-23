using EcommercePlatform.Application.Features.Orders.Commands.UpdateOrderStatus;

namespace EcommercePlatform.UnitTests.Application.Orders;

public sealed class UpdateOrderStatusValidatorTests
{
    private readonly UpdateOrderStatusValidator _validator = new();

    [Theory]
    [InlineData("Confirmed")]
    [InlineData("COMPLETED")]
    [InlineData("cancelled")]
    public void Validate_Should_Pass_When_CommandIsValid(string status)
    {
        var result = _validator.Validate(new UpdateOrderStatusCommand(Guid.NewGuid(), status));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_Fail_When_OrderIdIsEmpty()
    {
        var result = _validator.Validate(new UpdateOrderStatusCommand(Guid.Empty, "Completed"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateOrderStatusCommand.OrderId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("UnknownStatus")]
    public void Validate_Should_Fail_When_StatusIsInvalid(string status)
    {
        var result = _validator.Validate(new UpdateOrderStatusCommand(Guid.NewGuid(), status));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateOrderStatusCommand.Status));
    }
}
