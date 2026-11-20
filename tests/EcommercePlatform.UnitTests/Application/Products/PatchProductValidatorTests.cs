using EcommercePlatform.Application.Features.Products.Commands.PatchProduct;

namespace EcommercePlatform.UnitTests.Application.Products;

public sealed class PatchProductValidatorTests
{
    private readonly PatchProductValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_OnlyOneFieldIsProvided()
    {
        var command = new PatchProductCommand(Guid.NewGuid(), Price: 199m);

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_Fail_When_NoFieldIsProvided()
    {
        var command = new PatchProductCommand(Guid.NewGuid());

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_Should_Fail_When_ProvidedFieldsAreInvalid()
    {
        var emptyName = _validator.Validate(new PatchProductCommand(Guid.NewGuid(), Name: "   "));
        var negativePrice = _validator.Validate(new PatchProductCommand(Guid.NewGuid(), Price: -10m));
        var emptyCategory = _validator.Validate(new PatchProductCommand(Guid.NewGuid(), CategoryId: Guid.Empty));

        Assert.False(emptyName.IsValid);
        Assert.False(negativePrice.IsValid);
        Assert.False(emptyCategory.IsValid);
    }
}
