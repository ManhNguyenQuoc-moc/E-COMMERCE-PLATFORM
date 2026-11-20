using EcommercePlatform.Application.Features.Orders.Queries.GetOrders;

namespace EcommercePlatform.UnitTests.Application.Orders;

public sealed class GetOrdersQueryValidatorTests
{
    private readonly GetOrdersQueryValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_QueryIsValid()
    {
        var result = _validator.Validate(new GetOrdersQuery(1, 10));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void Validate_Should_Fail_When_PaginationIsOutOfRange(int page, int size)
    {
        var result = _validator.Validate(new GetOrdersQuery(page, size));

        Assert.False(result.IsValid);
    }
}
