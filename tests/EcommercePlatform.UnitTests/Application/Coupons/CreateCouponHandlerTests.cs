using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Common.Mappings;
using EcommercePlatform.Application.Features.Coupons.Commands.CreateCoupon;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EcommercePlatform.UnitTests.Application.Coupons;

public sealed class CreateCouponHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<Coupon> _couponRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateCouponHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _couponRepository = new Repository<Coupon>(_dbContext);
        _unitOfWork = _dbContext;

        var config = new TypeAdapterConfig();
        new CouponMapProfile().Register(config);
        _mapper = new Mapper(config);
    }

    [Fact]
    public async Task Handle_Should_CreateCoupon_When_CodeIsUnique()
    {
        var handler = new CreateCouponHandler(_couponRepository, _unitOfWork, _mapper);
        var command = new CreateCouponCommand(
            "welcome50",
            "FIXED_AMOUNT",
            50_000m,
            200_000m,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(10),
            20);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("WELCOME50", result.Code);
        Assert.Equal("FixedAmount", result.DiscountType);
        Assert.Equal(50_000m, result.Value);
        Assert.Equal(0, result.UsedCount);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task Handle_Should_ThrowDomainException_When_CodeAlreadyExists()
    {
        var existing = new Coupon(
            "DUP10",
            DiscountType.Percentage,
            10m,
            0m,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(5),
            10);
        _dbContext.Coupons.Add(existing);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateCouponHandler(_couponRepository, _unitOfWork, _mapper);
        var command = new CreateCouponCommand(
            "dup10",
            "PERCENTAGE",
            10m,
            0m,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(5),
            10);

        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command, CancellationToken.None));
    }

    public void Dispose() => _dbContext.Dispose();
}
