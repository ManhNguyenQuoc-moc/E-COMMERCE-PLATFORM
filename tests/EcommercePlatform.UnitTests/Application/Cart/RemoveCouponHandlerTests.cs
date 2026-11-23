using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Cart.Commands.RemoveCoupon;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using CartEntity = EcommercePlatform.Domain.Entities.Cart;

namespace EcommercePlatform.UnitTests.Application.Cart;

public sealed class RemoveCouponHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<CartEntity> _cartRepository;
    private readonly IRepository<Coupon> _couponRepository;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _userId = Guid.NewGuid();

    public RemoveCouponHandlerTests()
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
    public async Task Handle_Should_ClearAppliedCoupon_And_ResetDiscountAmount()
    {
        var product = new Product("Serum", "30ml", "SER-01", 250_000m, 10);
        _dbContext.Products.Add(product);

        var coupon = new Coupon(
            "SAVE50K",
            DiscountType.FixedAmount,
            50_000m,
            100_000m,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(5),
            10);
        _dbContext.Coupons.Add(coupon);

        var cart = new CartEntity(_userId);
        cart.AddOrIncrementItem(product.Id, 2, product.StockQuantity);
        cart.ApplyCoupon(coupon.Id);
        _dbContext.Carts.Add(cart);
        await _dbContext.SaveChangesAsync();

        var handler = new RemoveCouponHandler(_cartRepository, _couponRepository, _currentUserMock.Object, _dbContext);
        var result = await handler.Handle(new RemoveCouponCommand(), CancellationToken.None);

        Assert.Null(result.AppliedCouponId);
        Assert.Equal(500_000m, result.SubTotal);
        Assert.Equal(0m, result.DiscountAmount);
        Assert.Equal(500_000m, result.TotalAmount);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmptyCart_When_CartDoesNotExist()
    {
        var handler = new RemoveCouponHandler(_cartRepository, _couponRepository, _currentUserMock.Object, _dbContext);
        var result = await handler.Handle(new RemoveCouponCommand(), CancellationToken.None);

        Assert.Null(result.AppliedCouponId);
        Assert.Empty(result.Items);
    }

    public void Dispose() => _dbContext.Dispose();
}
