using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Common.Mappings;
using EcommercePlatform.Application.Features.Orders.Queries.GetOrders;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EcommercePlatform.UnitTests.Application.Orders;

public sealed class GetOrdersHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<Order> _orderRepository;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly IMapper _mapper;
    private readonly Guid _userId = Guid.NewGuid();

    public GetOrdersHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _orderRepository = new Repository<Order>(_dbContext);

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.UserGuid).Returns(_userId);

        var config = new TypeAdapterConfig();
        new OrderMapProfile().Register(config);
        _mapper = new Mapper(config);
    }

    [Fact]
    public async Task Handle_Should_ReturnPaginatedOrdersForCurrentUserOnly()
    {
        var order1 = new Order(_userId, "ORD-01", "Alice", "0901234567", "123 Street");
        order1.AddItem(Guid.NewGuid(), "Serum C", "SER-01", 200_000m, 1);

        var order2 = new Order(_userId, "ORD-02", "Alice", "0901234567", "123 Street");
        order2.AddItem(Guid.NewGuid(), "Toner", "TON-01", 100_000m, 2);

        var otherUserOrder = new Order(Guid.NewGuid(), "ORD-03", "Bob", "0909999999", "456 Avenue");
        otherUserOrder.AddItem(Guid.NewGuid(), "Cream", "CRM-01", 300_000m, 1);

        _dbContext.Orders.AddRange(order1, order2, otherUserOrder);
        await _dbContext.SaveChangesAsync();

        var handler = new GetOrdersHandler(_orderRepository, _currentUserMock.Object, _mapper);
        var result = await handler.Handle(new GetOrdersQuery(1, 10), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, o => Assert.Equal(_userId, o.UserId));
        Assert.All(result.Items, o => Assert.Single(o.Items));
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorizedAccessException_When_UserIsNotAuthenticated()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = new GetOrdersHandler(_orderRepository, _currentUserMock.Object, _mapper);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new GetOrdersQuery(), CancellationToken.None));
    }

    public void Dispose() => _dbContext.Dispose();
}
