using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Cart.Commands.UpdateCartItem;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using CartEntity = EcommercePlatform.Domain.Entities.Cart;

namespace EcommercePlatform.UnitTests.Application.Cart;

public sealed class UpdateCartItemHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<CartEntity> _cartRepository;
    private readonly IRepository<CartItem> _cartItemRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Guid _userId = Guid.NewGuid();

    public UpdateCartItemHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _cartRepository = new Repository<CartEntity>(_dbContext);
        _cartItemRepository = new Repository<CartItem>(_dbContext);
        _productRepository = new Repository<Product>(_dbContext);

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.UserGuid).Returns(_userId);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct => _dbContext.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task Handle_Should_UpdateQuantity_When_StockIsSufficient()
    {
        var product = new Product("Serum", "Desc", "SRM-1", 100m, 10);
        _dbContext.Products.Add(product);

        var cart = new CartEntity(_userId);
        cart.AddOrIncrementItem(product.Id, 2, product.StockQuantity);
        _dbContext.Carts.Add(cart);
        await _dbContext.SaveChangesAsync();

        var itemId = cart.Items.First().Id;
        var handler = new UpdateCartItemHandler(_cartRepository, _cartItemRepository, _productRepository, _currentUserMock.Object, _unitOfWorkMock.Object);

        var result = await handler.Handle(new UpdateCartItemCommand(itemId, 5), CancellationToken.None);

        Assert.Equal(5, result.TotalItems);
        Assert.Equal(500m, result.SubTotal);
    }

    [Fact]
    public async Task Handle_Should_RemoveItemFromDatabase_When_QuantityIsZero()
    {
        var product = new Product("Serum", "Desc", "SRM-1", 100m, 10);
        _dbContext.Products.Add(product);

        var cart = new CartEntity(_userId);
        cart.AddOrIncrementItem(product.Id, 2, product.StockQuantity);
        _dbContext.Carts.Add(cart);
        await _dbContext.SaveChangesAsync();

        var itemId = cart.Items.First().Id;
        var handler = new UpdateCartItemHandler(_cartRepository, _cartItemRepository, _productRepository, _currentUserMock.Object, _unitOfWorkMock.Object);

        var result = await handler.Handle(new UpdateCartItemCommand(itemId, 0), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItems);
        Assert.Null(await _dbContext.CartItems.FirstOrDefaultAsync(i => i.Id == itemId));
    }

    [Fact]
    public async Task Handle_Should_ThrowDomainException_When_RequestedQuantityExceedsStock()
    {
        var product = new Product("Serum", "Desc", "SRM-1", 100m, 3);
        _dbContext.Products.Add(product);

        var cart = new CartEntity(_userId);
        cart.AddOrIncrementItem(product.Id, 1, product.StockQuantity);
        _dbContext.Carts.Add(cart);
        await _dbContext.SaveChangesAsync();

        var itemId = cart.Items.First().Id;
        var handler = new UpdateCartItemHandler(_cartRepository, _cartItemRepository, _productRepository, _currentUserMock.Object, _unitOfWorkMock.Object);

        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            new UpdateCartItemCommand(itemId, 4),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_CartItemBelongsToAnotherUserOrMissing()
    {
        var product = new Product("Serum", "Desc", "SRM-1", 100m, 5);
        _dbContext.Products.Add(product);

        var otherCart = new CartEntity(Guid.NewGuid());
        otherCart.AddOrIncrementItem(product.Id, 1, product.StockQuantity);
        _dbContext.Carts.Add(otherCart);
        await _dbContext.SaveChangesAsync();

        var otherItemId = otherCart.Items.First().Id;
        var handler = new UpdateCartItemHandler(_cartRepository, _cartItemRepository, _productRepository, _currentUserMock.Object, _unitOfWorkMock.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new UpdateCartItemCommand(otherItemId, 2),
            CancellationToken.None));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
