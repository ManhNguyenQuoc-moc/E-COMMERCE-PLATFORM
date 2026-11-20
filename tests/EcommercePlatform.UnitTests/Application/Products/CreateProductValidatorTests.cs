using EcommercePlatform.Application.Features.Products.Commands.CreateProduct;

namespace EcommercePlatform.UnitTests.Application.Products;

public sealed class CreateProductValidatorTests
{
    private readonly CreateProductValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        var command = new CreateProductCommand("Laptop", "Gaming laptop", "LAP-01", 1200m, 5, Guid.NewGuid());

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_Fail_When_CategoryIdIsEmptyGuid()
    {
        var command = new CreateProductCommand("Laptop", "Gaming laptop", "LAP-01", 1200m, 5, Guid.Empty);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductCommand.CategoryId));
    }

    [Theory]
    [InlineData("", "SKU-1", 10, 1)]
    [InlineData("Name", "", 10, 1)]
    [InlineData("Name", "SKU-1", -1, 1)]
    [InlineData("Name", "SKU-1", 10, -1)]
    public void Validate_Should_Fail_When_RequiredOrRangeRulesViolated(
        string name,
        string sku,
        decimal price,
        int stock)
    {
        var command = new CreateProductCommand(name, "Desc", sku, price, stock);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
