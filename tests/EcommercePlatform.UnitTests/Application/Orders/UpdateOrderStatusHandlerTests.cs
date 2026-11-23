using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Common.Mappings;
using EcommercePlatform.Application.Features.Orders.Commands.UpdateOrderStatus;
using EcommercePlatform.Application.Features.Orders.DTOs;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EcommercePlatform.UnitTests.Application.Orders;

public sealed class UpdateOrderStatusHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<LoyaltyPointHistory> _historyRepository;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly IMapper _mapper;
    private readonly User _user;

    public UpdateOrderStatusHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _orderRepository = new Repository<Order>(_dbContext);
        _userRepository = new Repository<User>(_dbContext);
        _historyRepository = new Repository<LoyaltyPointHistory>(_dbContext);

        _user = new User(Guid.NewGuid(), "Alice", "0901234567", "123 Street");
        _dbContext.Users.Add(_user);
        _dbContext.SaveChanges();

        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<OrderDto>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<OrderDto>>, CancellationToken>(
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
    public async Task Handle_Should_AwardLoyaltyPoints_UpdateRank_And_CreateHistory_When_Completed()
    {
        var order = new Order(_user.Id, "ORD-100", "Alice", "0901234567", "123 Street");
        order.AddItem(Guid.NewGuid(), "Cream", "CRM-01", 10_500_000m, 1); // 10,500,000 -> 105 points -> Silver
        order.ApplyDiscounts(_user.Rank, 0m);
        AdvanceToShipped(order);
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateOrderStatusHandler(
            _orderRepository,
            _userRepository,
            _historyRepository,
            _unitOfWorkMock.Object,
            _mapper);

        var result = await handler.Handle(new UpdateOrderStatusCommand(order.Id, "COMPLETED"), CancellationToken.None);

        Assert.Equal("Completed", result.Status);

        var updatedUser = await _dbContext.Users.FindAsync(_user.Id);
        Assert.Equal(105, updatedUser!.LoyaltyPoints);
        Assert.Equal(UserRank.Silver, updatedUser.Rank);

        var histories = await _dbContext.LoyaltyPointHistories.Where(h => h.OrderId == order.Id).ToListAsync();
        Assert.Single(histories);
        Assert.Equal(105, histories[0].Points);
    }

    [Fact]
    public async Task Handle_Should_ThrowDomainException_When_OrderIsAlreadyCompleted()
    {
        var order = new Order(_user.Id, "ORD-101", "Alice", "0901234567", "123 Street");
        order.AddItem(Guid.NewGuid(), "Serum", "SER-01", 200_000m, 1);
        AdvanceToShipped(order);
        order.UpdateStatus(OrderStatus.Completed);
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateOrderStatusHandler(
            _orderRepository,
            _userRepository,
            _historyRepository,
            _unitOfWorkMock.Object,
            _mapper);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new UpdateOrderStatusCommand(order.Id, "Cancelled"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_CompleteWithoutHistory_When_OrderTotalIsBelow100k()
    {
        var order = new Order(_user.Id, "ORD-102", "Alice", "0901234567", "123 Street");
        order.AddItem(Guid.NewGuid(), "Mask", "MSK-01", 50_000m, 1); // < 100,000 -> 0 points
        order.ApplyDiscounts(_user.Rank, 0m);
        AdvanceToShipped(order);
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateOrderStatusHandler(
            _orderRepository,
            _userRepository,
            _historyRepository,
            _unitOfWorkMock.Object,
            _mapper);

        var result = await handler.Handle(new UpdateOrderStatusCommand(order.Id, "Completed"), CancellationToken.None);

        Assert.Equal("Completed", result.Status);
        var updatedUser = await _dbContext.Users.FindAsync(_user.Id);
        Assert.Equal(0, updatedUser!.LoyaltyPoints);
        Assert.Equal(UserRank.Member, updatedUser.Rank);
        Assert.Empty(await _dbContext.LoyaltyPointHistories.Where(h => h.OrderId == order.Id).ToListAsync());
    }

    [Fact]
    public async Task Handle_Should_PromoteUserToGoldAndDiamond_When_PointsCrossThresholds()
    {
        var order1 = new Order(_user.Id, "ORD-103", "Alice", "0901234567", "123 Street");
        order1.AddItem(Guid.NewGuid(), "Set A", "SET-A", 50_000_000m, 1); // 500 points -> Gold
        order1.ApplyDiscounts(_user.Rank, 0m);
        AdvanceToShipped(order1);

        var order2 = new Order(_user.Id, "ORD-104", "Alice", "0901234567", "123 Street");
        order2.AddItem(Guid.NewGuid(), "Set B", "SET-B", 50_000_000m, 1); // +500 = 1000 points -> Diamond
        order2.ApplyDiscounts(_user.Rank, 0m);
        AdvanceToShipped(order2);

        _dbContext.Orders.AddRange(order1, order2);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateOrderStatusHandler(
            _orderRepository,
            _userRepository,
            _historyRepository,
            _unitOfWorkMock.Object,
            _mapper);

        await handler.Handle(new UpdateOrderStatusCommand(order1.Id, "Completed"), CancellationToken.None);
        var userAfterOrder1 = await _dbContext.Users.FindAsync(_user.Id);
        Assert.Equal(UserRank.Gold, userAfterOrder1!.Rank);

        await handler.Handle(new UpdateOrderStatusCommand(order2.Id, "Completed"), CancellationToken.None);
        var userAfterOrder2 = await _dbContext.Users.FindAsync(_user.Id);
        Assert.Equal(1000, userAfterOrder2!.LoyaltyPoints);
        Assert.Equal(UserRank.Diamond, userAfterOrder2.Rank);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, "Completed")]
    [InlineData(OrderStatus.Pending, "Shipped")]
    [InlineData(OrderStatus.Confirmed, "Completed")]
    [InlineData(OrderStatus.Shipped, "Confirmed")]
    [InlineData(OrderStatus.Shipped, "Cancelled")]
    public async Task Handle_Should_ThrowDomainException_When_StatusTransitionSkipsOrReverts(
        OrderStatus initialStatus,
        string targetStatus)
    {
        var order = new Order(_user.Id, $"ORD-{Guid.NewGuid():N}"[..12], "Alice", "0901234567", "123 Street");
        order.AddItem(Guid.NewGuid(), "Serum", "SER-01", 200_000m, 1);

        if (initialStatus >= OrderStatus.Confirmed) order.UpdateStatus(OrderStatus.Confirmed);
        if (initialStatus >= OrderStatus.Processing) order.UpdateStatus(OrderStatus.Processing);
        if (initialStatus >= OrderStatus.Shipped) order.UpdateStatus(OrderStatus.Shipped);

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateOrderStatusHandler(
            _orderRepository,
            _userRepository,
            _historyRepository,
            _unitOfWorkMock.Object,
            _mapper);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new UpdateOrderStatusCommand(order.Id, targetStatus), CancellationToken.None));
    }

    private static void AdvanceToShipped(Order order)
    {
        order.UpdateStatus(OrderStatus.Confirmed);
        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);
    }

    public void Dispose() => _dbContext.Dispose();
}
