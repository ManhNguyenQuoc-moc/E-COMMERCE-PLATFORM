using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.UnitTests.Domain;

public sealed class CategoryTests
{
    [Fact]
    public void Constructor_Should_NormalizeSlugAndTrimName_When_Valid()
    {
        var category = new Category("  Electronics  ", "  ELECTRONICS-TECH  ", "  Devices  ");

        Assert.Equal("Electronics", category.Name);
        Assert.Equal("electronics-tech", category.Slug);
        Assert.Equal("Devices", category.Description);
        Assert.False(category.IsDeleted);
        Assert.Null(category.DeletedAt);
    }

    [Theory]
    [InlineData("", "slug")]
    [InlineData("   ", "slug")]
    [InlineData("Name", "")]
    [InlineData("Name", "   ")]
    public void Constructor_Should_ThrowDomainException_When_Invalid(string name, string slug)
    {
        Assert.Throws<DomainException>(() => new Category(name, slug));
    }

    [Fact]
    public void UpdateDetails_Should_UpdateFieldsAndTimestamp()
    {
        var category = new Category("Old", "old", "Old desc");

        category.UpdateDetails("  New Name ", " NEW-SLUG ", " New desc ");

        Assert.Equal("New Name", category.Name);
        Assert.Equal("new-slug", category.Slug);
        Assert.Equal("New desc", category.Description);
        Assert.NotNull(category.UpdatedAt);
    }

    [Fact]
    public void SoftDelete_Should_MarkAsDeleted_And_ThrowWhenAlreadyDeleted()
    {
        var category = new Category("Books", "books");

        category.SoftDelete();

        Assert.True(category.IsDeleted);
        Assert.NotNull(category.DeletedAt);
        Assert.Throws<DomainException>(() => category.SoftDelete());
    }
}
