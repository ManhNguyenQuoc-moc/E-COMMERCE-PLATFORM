using EcommercePlatform.Application.Features.Products.Queries.GetProducts;

namespace EcommercePlatform.UnitTests.Application.Products;

public sealed class GetProductsValidatorTests
{
    private readonly GetProductsValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_QueryIsValid()
    {
        var query = new GetProductsQuery(Page: 1, Size: 20, CategoryId: Guid.NewGuid(), Search: "laptop", MinPrice: 100m, MaxPrice: 500m);

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0, 10, null, null)]
    [InlineData(1, 0, null, null)]
    [InlineData(1, 101, null, null)]
    [InlineData(1, 10, -1.0, null)]
    [InlineData(1, 10, null, -5.0)]
    [InlineData(1, 10, 500.0, 100.0)]
    public void Validate_Should_Fail_When_ParametersAreOutOfRange(
        int page,
        int size,
        double? minPrice,
        double? maxPrice)
    {
        var query = new GetProductsQuery(
            Page: page,
            Size: size,
            MinPrice: minPrice.HasValue ? (decimal)minPrice.Value : null,
            MaxPrice: maxPrice.HasValue ? (decimal)maxPrice.Value : null);

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
    }
}
