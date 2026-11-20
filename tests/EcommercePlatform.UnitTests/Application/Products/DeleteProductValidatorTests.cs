using EcommercePlatform.Application.Features.Products.Commands.DeleteProduct;

namespace EcommercePlatform.UnitTests.Application.Products;

public sealed class DeleteProductValidatorTests
{
    private readonly DeleteProductValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_IdIsValid()
    {
        var result = _validator.Validate(new DeleteProductCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_Fail_When_IdIsEmptyGuid()
    {
        var result = _validator.Validate(new DeleteProductCommand(Guid.Empty));

        Assert.False(result.IsValid);
    }
}
