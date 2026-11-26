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
    private readonly IRepository<Coupon> _couponRepository;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _userId = Guid.NewGuid();

    public GetCartHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _cartRepository = new Repository<CartEntity>(_dbContext);
        _couponRepository = new Repository<Coupon>(_dbContext);

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.UserGuid).Returns(_userId);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmptyCart_When_UserHasNoCartYet()
    {
        var handler = new GetCartHandler(_cartRepository, _couponRepository, _currentUserMock.Object);

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

        var handler = new GetCartHandler(_cartRepository, _couponRepository, _currentUserMock.Object);
        var result = await handler.Handle(new GetCartQuery(), CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(3, result.TotalItems);
        Assert.Equal(500m, result.SubTotal);
        Assert.Equal(500m, result.TotalAmount);
    }

    [Fact]
    public async Task Handle_Should_IncludeUserRankDiscount_When_UserRepositoryProvided()
    {
        var account = new Account("gold_user", "gold@example.com", "hashed_pwd");
        var user = new User(account.Id, "Gold User");
        user.AddLoyaltyPoints(500); // Gold = 5%
        var product = new Product("Serum", "30ml", "SRM-1", 100_000m, 10);

        _dbContext.Accounts.Add(account);
        _dbContext.Users.Add(user);
        _dbContext.Products.Add(product);

        var cart = new CartEntity(user.Id);
        cart.AddOrIncrementItem(product.Id, 2, product.StockQuantity);
        _dbContext.Carts.Add(cart);
        await _dbContext.SaveChangesAsync();

        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        currentUserMock.Setup(u => u.UserGuid).Returns(user.Id);

        var userRepository = new Repository<User>(_dbContext);
        var handler = new GetCartHandler(_cartRepository, _couponRepository, currentUserMock.Object, userRepository);
        var result = await handler.Handle(new GetCartQuery(), CancellationToken.None);

        Assert.Equal(200_000m, result.SubTotal);
        Assert.Equal(10_000m, result.DiscountAmount);
        Assert.Equal(190_000m, result.TotalAmount);
    }

    [Fact]
    public async Task Handle_Should_ShowSoftDeletedItemAsUnavailable_And_ExcludeFromSubTotal()
    {
        var activeProduct = new Product("Cleanser", "100ml", "CLN-01", 150_000m, 10);
        var deletedProduct = new Product("Sunscreen", "50ml", "SUN-01", 200_000m, 8);
        _dbContext.Products.AddRange(activeProduct, deletedProduct);

        var cart = new CartEntity(_userId);
        cart.AddOrIncrementItem(activeProduct.Id, 2, activeProduct.StockQuantity);
        cart.AddOrIncrementItem(deletedProduct.Id, 1, deletedProduct.StockQuantity);
        _dbContext.Carts.Add(cart);
        deletedProduct.SoftDelete();
        await _dbContext.SaveChangesAsync();

        var handler = new GetCartHandler(_cartRepository, _couponRepository, _currentUserMock.Object);
        var result = await handler.Handle(new GetCartQuery(), CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        var activeItem = result.Items.Single(i => i.ProductId == activeProduct.Id);
        var unavailableItem = result.Items.Single(i => i.ProductId == deletedProduct.Id);

        Assert.True(activeItem.IsAvailable);
        Assert.False(unavailableItem.IsAvailable);
        Assert.Equal(0, unavailableItem.AvailableStock);
        Assert.Equal(300_000m, result.SubTotal);
        Assert.Equal(300_000m, result.TotalAmount);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
