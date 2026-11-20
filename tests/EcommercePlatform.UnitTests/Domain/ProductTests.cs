using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.UnitTests.Domain;

public sealed class ProductTests
{
    [Fact]
    public void Constructor_Should_CreateActiveProduct_When_StockIsPositive()
    {
        var categoryId = Guid.NewGuid();
        var product = new Product("  Laptop Pro  ", "  High-end laptop  ", " lap-001 ", 1500m, 10, categoryId);

        Assert.Equal("Laptop Pro", product.Name);
        Assert.Equal("High-end laptop", product.Description);
        Assert.Equal("LAP-001", product.Sku);
        Assert.Equal(1500m, product.Price);
        Assert.Equal(10, product.StockQuantity);
        Assert.Equal(categoryId, product.CategoryId);
        Assert.Equal(ProductStatus.Active, product.Status);
        Assert.False(product.IsDeleted);
        Assert.Null(product.DeletedAt);
    }

    [Fact]
    public void Constructor_Should_CreateOutOfStockProduct_When_StockIsZero()
    {
        var product = new Product("Phone", "Smart phone", "phn-001", 999m, 0);

        Assert.Equal(ProductStatus.OutOfStock, product.Status);
        Assert.Null(product.CategoryId);
    }

    [Theory]
    [InlineData("", "SKU-1", 10, 1)]
    [InlineData("   ", "SKU-1", 10, 1)]
    [InlineData("Name", "", 10, 1)]
    [InlineData("Name", "SKU-1", -1, 1)]
    [InlineData("Name", "SKU-1", 10, -1)]
    public void Constructor_Should_ThrowDomainException_When_ArgumentsAreInvalid(
        string name,
        string sku,
        decimal price,
        int stock)
    {
        Assert.Throws<DomainException>(() => new Product(name, "Desc", sku, price, stock));
    }

    [Fact]
    public void UpdateDetails_Should_UpdatePropertiesAndTimestamp_When_Valid()
    {
        var categoryId = Guid.NewGuid();
        var product = new Product("Old Name", "Old Desc", "SKU-1", 100m, 5);

        product.UpdateDetails("  New Name  ", "  New Desc  ", 250m, categoryId);

        Assert.Equal("New Name", product.Name);
        Assert.Equal("New Desc", product.Description);
        Assert.Equal(250m, product.Price);
        Assert.Equal(categoryId, product.CategoryId);
        Assert.NotNull(product.UpdatedAt);
    }

    [Fact]
    public void AdjustStock_Should_ToggleStatusAndThrowOnInsufficientStock()
    {
        var product = new Product("Mouse", "Wireless mouse", "MOU-1", 50m, 5);

        product.AdjustStock(-5);
        Assert.Equal(0, product.StockQuantity);
        Assert.Equal(ProductStatus.OutOfStock, product.Status);

        product.AdjustStock(3);
        Assert.Equal(3, product.StockQuantity);
        Assert.Equal(ProductStatus.Active, product.Status);

        Assert.Throws<DomainException>(() => product.AdjustStock(-4));
    }

    [Fact]
    public void SoftDelete_Should_MarkAsDeleted_And_ThrowOnSecondCall()
    {
        var product = new Product("Keyboard", "Mechanical", "KEY-1", 120m, 4);

        product.SoftDelete();

        Assert.True(product.IsDeleted);
        Assert.NotNull(product.DeletedAt);
        Assert.NotNull(product.UpdatedAt);

        Assert.Throws<DomainException>(() => product.SoftDelete());
    }
}
