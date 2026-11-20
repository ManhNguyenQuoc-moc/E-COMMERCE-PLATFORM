using EcommercePlatform.Application.Features.Products.Queries.GetProductById;

namespace EcommercePlatform.UnitTests.Application.Products;

public sealed class GetProductByIdValidatorTests
{
    private readonly GetProductByIdValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_IdIsValid()
    {
        var result = _validator.Validate(new GetProductByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_Fail_When_IdIsEmptyGuid()
    {
        var result = _validator.Validate(new GetProductByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
    }
}
