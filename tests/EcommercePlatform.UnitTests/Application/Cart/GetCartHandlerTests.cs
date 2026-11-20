using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Cart.Queries.GetCart;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using CartEntity = EcommercePlatform.Domain.Entities.Cart;

namespace EcommercePlatform.UnitTests.Application.Cart;

public sealed class GetCartHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<CartEntity> _cartRepository;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _userId = Guid.NewGuid();

    public GetCartHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _cartRepository = new Repository<CartEntity>(_dbContext);

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.UserGuid).Returns(_userId);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmptyCart_When_UserHasNoCartYet()
    {
        var handler = new GetCartHandler(_cartRepository, _currentUserMock.Object);

        var result = await handler.Handle(new GetCartQuery(), CancellationToken.None);

        Assert.Equal(_userId, result.UserId);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItems);
        Assert.Equal(0m, result.SubTotal);
    }

    [Fact]
    public async Task Handle_Should_ReturnCartItemsAndTotals_ForCurrentUser()
    {
        var product1 = new Product("Cleanser", "100ml", "CLN-01", 150m, 10);
        var product2 = new Product("Sunscreen", "50ml", "SUN-01", 200m, 8);
        _dbContext.Products.AddRange(product1, product2);

        var cart = new CartEntity(_userId);
        cart.AddOrIncrementItem(product1.Id, 2, product1.StockQuantity);
        cart.AddOrIncrementItem(product2.Id, 1, product2.StockQuantity);
        _dbContext.Carts.Add(cart);
        await _dbContext.SaveChangesAsync();

        var handler = new GetCartHandler(_cartRepository, _currentUserMock.Object);
        var result = await handler.Handle(new GetCartQuery(), CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(3, result.TotalItems);
        Assert.Equal(500m, result.SubTotal);
        Assert.Equal(500m, result.TotalAmount);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
