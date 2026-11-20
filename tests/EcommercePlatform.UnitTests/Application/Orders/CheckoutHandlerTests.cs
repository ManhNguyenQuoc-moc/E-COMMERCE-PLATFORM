using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Common.Mappings;
using EcommercePlatform.Application.Features.Orders.Commands.Checkout;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using CartEntity = EcommercePlatform.Domain.Entities.Cart;

namespace EcommercePlatform.UnitTests.Application.Orders;

public sealed class CheckoutHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<CartEntity> _cartRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Order> _orderRepository;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly IMapper _mapper;
    private readonly User _user;

    public CheckoutHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _cartRepository = new Repository<CartEntity>(_dbContext);
        _productRepository = new Repository<Product>(_dbContext);
        _userRepository = new Repository<User>(_dbContext);
        _orderRepository = new Repository<Order>(_dbContext);

        _user = new User(Guid.NewGuid(), "Alice", "0901234567", "123 Street");
        _user.AddLoyaltyPoints(150); // Silver rank -> 2% discount
        _dbContext.Users.Add(_user);
        _dbContext.SaveChanges();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.UserGuid).Returns(_user.Id);

        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<EcommercePlatform.Application.Features.Orders.DTOs.OrderDto>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<EcommercePlatform.Application.Features.Orders.DTOs.OrderDto>>, CancellationToken>(
                async (op, ct) =>
                {
                    var res = await op(ct);
                    await _dbContext.SaveChangesAsync(ct);
                    return res;
                });

        var config = new TypeAdapterConfig();
        new OrderMapProfile().Register(config);
        _mapper = new Mapper(config);
    }

    [Fact]
    public async Task Handle_Should_CreateOrder_DeductStock_ApplyRankDiscount_And_ClearCart()
    {
        var product1 = new Product("Serum C", "30ml", "SER-01", 200_000m, 2);
        var product2 = new Product("Toner", "150ml", "TON-01", 100_000m, 5);
        _dbContext.Products.AddRange(product1, product2);

        var cart = new CartEntity(_user.Id);
        cart.AddOrIncrementItem(product1.Id, 2, product1.StockQuantity);
        cart.AddOrIncrementItem(product2.Id, 1, product2.StockQuantity);
        _dbContext.Carts.Add(cart);
        await _dbContext.SaveChangesAsync();

        var handler = CreateHandler();
        var command = new CheckoutCommand("Alice", "0901234567", "123 Street", "Fast delivery");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(_user.Id, result.UserId);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(500_000m, result.SubTotal);
        Assert.Equal(10_000m, result.RankDiscountAmount); // 2% Silver
        Assert.Equal(490_000m, result.TotalAmount);
        Assert.Equal(2, result.Items.Count);

        var updatedProduct1 = await _dbContext.Products.FindAsync(product1.Id);
        var updatedProduct2 = await _dbContext.Products.FindAsync(product2.Id);
        Assert.Equal(0, updatedProduct1!.StockQuantity);
        Assert.Equal(ProductStatus.OutOfStock, updatedProduct1.Status);
        Assert.Equal(4, updatedProduct2!.StockQuantity);

        var updatedCart = await _dbContext.Carts.Include(c => c.Items).FirstAsync(c => c.UserId == _user.Id);
        Assert.Empty(updatedCart.Items);
    }

    [Fact]
    public async Task Handle_Should_ThrowDomainException_When_ProductInCartIsSoftDeleted()
    {
        var product = new Product("Serum C", "30ml", "SER-01", 200_000m, 5);
        _dbContext.Products.Add(product);

        var cart = new CartEntity(_user.Id);
        cart.AddOrIncrementItem(product.Id, 1, product.StockQuantity);
        _dbContext.Carts.Add(cart);
        product.SoftDelete();
        await _dbContext.SaveChangesAsync();

        var handler = CreateHandler();
        var command = new CheckoutCommand("Alice", "0901234567", "123 Street");

        var ex = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("no longer available", ex.Message);
    }

    [Fact]
    public async Task Handle_Should_ThrowDomainException_When_CartIsEmpty()
    {
        var handler = CreateHandler();
        var command = new CheckoutCommand("Alice", "0901234567", "123 Street");

        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_ThrowDomainException_When_StockIsInsufficient()
    {
        var product = new Product("Serum C", "30ml", "SER-01", 200_000m, 5);
        _dbContext.Products.Add(product);

        var cart = new CartEntity(_user.Id);
        cart.AddOrIncrementItem(product.Id, 4, product.StockQuantity);
        _dbContext.Carts.Add(cart);

        // Simulate concurrent purchase reducing stock before checkout
        product.AdjustStock(-3);
        await _dbContext.SaveChangesAsync();

        var handler = CreateHandler();
        var command = new CheckoutCommand("Alice", "0901234567", "123 Street");

        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorizedAccessException_When_UserIsNotAuthenticated()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = CreateHandler();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new CheckoutCommand("Alice", "0901234567", "123 Street"), CancellationToken.None));
    }

    private CheckoutHandler CreateHandler() => new(
        _cartRepository,
        _productRepository,
        _userRepository,
        _orderRepository,
        _currentUserMock.Object,
        _unitOfWorkMock.Object,
        _mapper);

    public void Dispose() => _dbContext.Dispose();
}
