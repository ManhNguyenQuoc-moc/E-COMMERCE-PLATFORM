using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.UnitTests.Domain;

public sealed class CartTests
{
    [Fact]
    public void Constructor_Should_ThrowDomainException_When_UserIdIsEmpty()
    {
        Assert.Throws<DomainException>(() => new Cart(Guid.Empty));
    }

    [Fact]
    public void AddOrIncrementItem_Should_AddNewItem_And_IncrementExistingItem()
    {
        var cart = new Cart(Guid.NewGuid());
        var productId = Guid.NewGuid();

        cart.AddOrIncrementItem(productId, 2, availableStock: 10);
        Assert.Single(cart.Items);
        Assert.Equal(2, cart.Items.First().Quantity);

        cart.AddOrIncrementItem(productId, 3, availableStock: 10);
        Assert.Single(cart.Items);
        Assert.Equal(5, cart.Items.First().Quantity);
    }

    [Fact]
    public void AddOrIncrementItem_Should_ThrowDomainException_When_ExceedingAvailableStock()
    {
        var cart = new Cart(Guid.NewGuid());
        var productId = Guid.NewGuid();

        cart.AddOrIncrementItem(productId, 4, availableStock: 5);

        Assert.Throws<DomainException>(() => cart.AddOrIncrementItem(productId, 2, availableStock: 5));
    }

    [Fact]
    public void AddOrIncrementItem_Should_ThrowDomainException_When_QuantityIsZeroOrNegative()
    {
        var cart = new Cart(Guid.NewGuid());

        Assert.Throws<DomainException>(() => cart.AddOrIncrementItem(Guid.NewGuid(), 0, 10));
        Assert.Throws<DomainException>(() => cart.AddOrIncrementItem(Guid.NewGuid(), -1, 10));
    }

    [Fact]
    public void AddOrIncrementItem_Should_ThrowInsufficientStock_When_SumOverflowsInt32()
    {
        var cart = new Cart(Guid.NewGuid());
        var productId = Guid.NewGuid();
        cart.AddOrIncrementItem(productId, 2, availableStock: 10);

        var ex = Assert.Throws<DomainException>(() =>
            cart.AddOrIncrementItem(productId, int.MaxValue, availableStock: 10));

        Assert.Contains("Insufficient stock", ex.Message);
    }

    [Fact]
    public void UpdateItemQuantity_Should_UpdateQuantity_And_ValidateStock()
    {
        var cart = new Cart(Guid.NewGuid());
        var productId = Guid.NewGuid();
        cart.AddOrIncrementItem(productId, 2, availableStock: 5);
        var itemId = cart.Items.First().Id;

        cart.UpdateItemQuantity(itemId, 4, availableStock: 5);
        Assert.Equal(4, cart.Items.First().Quantity);

        Assert.Throws<DomainException>(() => cart.UpdateItemQuantity(itemId, 6, availableStock: 5));
        Assert.Throws<DomainException>(() => cart.UpdateItemQuantity(itemId, 0, availableStock: 5));
    }

    [Fact]
    public void RemoveItem_Should_RemoveAndReturnItem_And_ThrowWhenMissing()
    {
        var cart = new Cart(Guid.NewGuid());
        var productId = Guid.NewGuid();
        cart.AddOrIncrementItem(productId, 2, availableStock: 5);
        var itemId = cart.Items.First().Id;

        var removedItem = cart.RemoveItem(itemId);
        Assert.Equal(itemId, removedItem.Id);
        Assert.Empty(cart.Items);

        Assert.Throws<NotFoundException>(() => cart.RemoveItem(Guid.NewGuid()));
    }

    [Fact]
    public void UpdateItemQuantity_Should_ThrowNotFoundException_When_ItemMissing()
    {
        var cart = new Cart(Guid.NewGuid());

        Assert.Throws<NotFoundException>(() => cart.UpdateItemQuantity(Guid.NewGuid(), 1, 10));
    }
}
