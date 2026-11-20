using EcommercePlatform.Application.Features.Products.Commands.UpdateProduct;

namespace EcommercePlatform.UnitTests.Application.Products;

public sealed class UpdateProductValidatorTests
{
    private readonly UpdateProductValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        var command = new UpdateProductCommand(Guid.NewGuid(), "Phone", "Updated desc", 800m, Guid.NewGuid());

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_Fail_When_IdOrCategoryIdIsEmptyGuid()
    {
        var emptyId = _validator.Validate(new UpdateProductCommand(Guid.Empty, "Phone", "Desc", 800m));
        var emptyCategory = _validator.Validate(new UpdateProductCommand(Guid.NewGuid(), "Phone", "Desc", 800m, Guid.Empty));

        Assert.False(emptyId.IsValid);
        Assert.False(emptyCategory.IsValid);
    }
}
