using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Cart.Commands.ApplyCoupon;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using CartEntity = EcommercePlatform.Domain.Entities.Cart;

namespace EcommercePlatform.UnitTests.Application.Cart;

public sealed class ApplyCouponHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<CartEntity> _cartRepository;
    private readonly IRepository<Coupon> _couponRepository;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _userId = Guid.NewGuid();

    public ApplyCouponHandlerTests()
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
    public async Task Handle_Should_ApplyCouponToCart_And_ComputeDiscountAmount()
    {
        var product = new Product("Serum", "30ml", "SER-01", 250_000m, 10);
        _dbContext.Products.Add(product);

        var coupon = new Coupon(
            "SAVE50K",
            DiscountType.FixedAmount,
            50_000m,
            200_000m,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(5),
            10);
        _dbContext.Coupons.Add(coupon);

        var cart = new CartEntity(_userId);
        cart.AddOrIncrementItem(product.Id, 2, product.StockQuantity);
        _dbContext.Carts.Add(cart);
        await _dbContext.SaveChangesAsync();

        var handler = new ApplyCouponHandler(_cartRepository, _couponRepository, _currentUserMock.Object, _dbContext);
        var result = await handler.Handle(new ApplyCouponCommand("save50k"), CancellationToken.None);

        Assert.Equal(coupon.Id, result.AppliedCouponId);
        Assert.Equal(500_000m, result.SubTotal);
        Assert.Equal(50_000m, result.DiscountAmount);
        Assert.Equal(450_000m, result.TotalAmount);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_CouponDoesNotExist()
    {
        var product = new Product("Serum", "30ml", "SER-01", 250_000m, 10);
        _dbContext.Products.Add(product);

        var cart = new CartEntity(_userId);
        cart.AddOrIncrementItem(product.Id, 1, product.StockQuantity);
        _dbContext.Carts.Add(cart);
        await _dbContext.SaveChangesAsync();

        var handler = new ApplyCouponHandler(_cartRepository, _couponRepository, _currentUserMock.Object, _dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ApplyCouponCommand("INVALID"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_ThrowDomainException_When_CartIsEmpty()
    {
        var handler = new ApplyCouponHandler(_cartRepository, _couponRepository, _currentUserMock.Object, _dbContext);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ApplyCouponCommand("SAVE50K"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_ThrowDomainException_When_CouponExpiredOrBelowMinOrder()
    {
        var product = new Product("Toner", "100ml", "TON-01", 100_000m, 10);
        _dbContext.Products.Add(product);

        var expiredCoupon = new Coupon(
            "EXPIRED",
            DiscountType.FixedAmount,
            20_000m,
            50_000m,
            DateTimeOffset.UtcNow.AddDays(-5),
            DateTimeOffset.UtcNow.AddDays(-1),
            10);
        var minOrderCoupon = new Coupon(
            "MIN500K",
            DiscountType.FixedAmount,
            50_000m,
            500_000m,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(5),
            10);
        _dbContext.Coupons.AddRange(expiredCoupon, minOrderCoupon);

        var cart = new CartEntity(_userId);
        cart.AddOrIncrementItem(product.Id, 1, product.StockQuantity);
        _dbContext.Carts.Add(cart);
        await _dbContext.SaveChangesAsync();

        var handler = new ApplyCouponHandler(_cartRepository, _couponRepository, _currentUserMock.Object, _dbContext);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ApplyCouponCommand("EXPIRED"), CancellationToken.None));
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ApplyCouponCommand("MIN500K"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_ThrowDomainException_When_CartContainsSoftDeletedProduct()
    {
        var activeProduct = new Product("Toner", "100ml", "TON-01", 300_000m, 10);
        var deletedProduct = new Product("Serum", "30ml", "SER-01", 250_000m, 10);
        _dbContext.Products.AddRange(activeProduct, deletedProduct);

        var coupon = new Coupon(
            "SAVE50K",
            DiscountType.FixedAmount,
            50_000m,
            200_000m,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(5),
            10);
        _dbContext.Coupons.Add(coupon);

        var cart = new CartEntity(_userId);
        cart.AddOrIncrementItem(activeProduct.Id, 1, activeProduct.StockQuantity);
        cart.AddOrIncrementItem(deletedProduct.Id, 1, deletedProduct.StockQuantity);
        _dbContext.Carts.Add(cart);
        deletedProduct.SoftDelete();
        await _dbContext.SaveChangesAsync();

        var handler = new ApplyCouponHandler(_cartRepository, _couponRepository, _currentUserMock.Object, _dbContext);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ApplyCouponCommand("SAVE50K"), CancellationToken.None));
        Assert.Contains("Serum", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("remove it from your cart", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose() => _dbContext.Dispose();
}
