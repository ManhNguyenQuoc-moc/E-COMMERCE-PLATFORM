using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Cart.Commands.AddToCart;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using CartEntity = EcommercePlatform.Domain.Entities.Cart;

namespace EcommercePlatform.UnitTests.Application.Cart;

public sealed class AddToCartHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<CartEntity> _cartRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Guid _userId = Guid.NewGuid();

    public AddToCartHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _cartRepository = new Repository<CartEntity>(_dbContext);
        _productRepository = new Repository<Product>(_dbContext);

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.UserGuid).Returns(_userId);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct => _dbContext.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task Handle_Should_CreateCartAndAddItem_And_IncrementOnSecondCall()
    {
        var product = new Product("Vitamin C Serum", "30ml", "SERUM-01", 320m, 5);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var handler = new AddToCartHandler(_cartRepository, _productRepository, _currentUserMock.Object, _unitOfWorkMock.Object);

        var firstResult = await handler.Handle(new AddToCartCommand(product.Id, 2), CancellationToken.None);
        Assert.Equal(_userId, firstResult.UserId);
        Assert.Single(firstResult.Items);
        Assert.Equal(2, firstResult.TotalItems);
        Assert.Equal(640m, firstResult.SubTotal);

        var secondResult = await handler.Handle(new AddToCartCommand(product.Id, 2), CancellationToken.None);
        Assert.Single(secondResult.Items);
        Assert.Equal(4, secondResult.TotalItems);
        Assert.Equal(1280m, secondResult.SubTotal);
    }

    [Fact]
    public async Task Handle_Should_AddSecondDistinctProductToExistingCart()
    {
        var product1 = new Product("Vitamin C Serum", "30ml", "SERUM-01", 320m, 5);
        var product2 = new Product("Sunscreen SPF50", "50ml", "SUN-01", 400m, 10);
        _dbContext.Products.AddRange(product1, product2);
        await _dbContext.SaveChangesAsync();

        var handler = new AddToCartHandler(_cartRepository, _productRepository, _currentUserMock.Object, _unitOfWorkMock.Object);

        await handler.Handle(new AddToCartCommand(product1.Id, 1), CancellationToken.None);
        var result = await handler.Handle(new AddToCartCommand(product2.Id, 2), CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(3, result.TotalItems);
        Assert.Equal(1120m, result.SubTotal);
    }

    [Fact]
    public async Task Handle_Should_ThrowDomainException_When_CumulativeQuantityExceedsStock()
    {
        var product = new Product("Toner", "150ml", "TONER-01", 250m, 3);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var handler = new AddToCartHandler(_cartRepository, _productRepository, _currentUserMock.Object, _unitOfWorkMock.Object);
        await handler.Handle(new AddToCartCommand(product.Id, 2), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            new AddToCartCommand(product.Id, 2),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_ProductMissingOrSoftDeleted()
    {
        var deleted = new Product("Old", "Desc", "OLD-01", 100m, 5);
        deleted.SoftDelete();
        _dbContext.Products.Add(deleted);
        await _dbContext.SaveChangesAsync();

        var handler = new AddToCartHandler(_cartRepository, _productRepository, _currentUserMock.Object, _unitOfWorkMock.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new AddToCartCommand(Guid.NewGuid(), 1),
            CancellationToken.None));

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new AddToCartCommand(deleted.Id, 1),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorizedAccessException_When_UserNotAuthenticated()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = new AddToCartHandler(_cartRepository, _productRepository, _currentUserMock.Object, _unitOfWorkMock.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(
            new AddToCartCommand(Guid.NewGuid(), 1),
            CancellationToken.None));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
