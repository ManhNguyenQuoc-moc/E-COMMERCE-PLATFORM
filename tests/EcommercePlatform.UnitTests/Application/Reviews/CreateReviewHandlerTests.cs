using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Common.Mappings;
using EcommercePlatform.Application.Features.Reviews.Commands.CreateReview;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EcommercePlatform.UnitTests.Application.Reviews;

public sealed class CreateReviewHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<Review> _reviewRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<User> _userRepository;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly IMapper _mapper;
    private readonly User _user;
    private readonly Product _product;

    public CreateReviewHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _reviewRepository = new Repository<Review>(_dbContext);
        _productRepository = new Repository<Product>(_dbContext);
        _orderRepository = new Repository<Order>(_dbContext);
        _userRepository = new Repository<User>(_dbContext);

        _user = new User(Guid.NewGuid(), "Alice Nguyen", "0901234567", "123 Street");
        _product = new Product("Vitamin C Serum", "30ml", "SER-01", 250_000m, 10);
        _dbContext.Users.Add(_user);
        _dbContext.Products.Add(_product);
        _dbContext.SaveChanges();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.UserGuid).Returns(_user.Id);

        var config = new TypeAdapterConfig();
        new ReviewMapProfile().Register(config);
        _mapper = new Mapper(config);
    }

    [Fact]
    public async Task Handle_Should_CreateReview_When_UserHasCompletedOrderForProduct()
    {
        var order = CreateOrderWithStatus(_user.Id, _product, OrderStatus.Completed);
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateReviewCommand(_product.Id, 5, "Awesome!"), CancellationToken.None);

        Assert.Equal(_product.Id, result.ProductId);
        Assert.Equal(_user.Id, result.UserId);
        Assert.Equal("Alice Nguyen", result.UserFullName);
        Assert.Equal(order.Id, result.OrderId);
        Assert.Equal(5, result.Rating);
        Assert.Equal("Awesome!", result.Comment);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_ProductDoesNotExist()
    {
        var handler = CreateHandler();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CreateReviewCommand(Guid.NewGuid(), 5, "Great"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_ThrowDomainException_When_OrderIsNotCompleted()
    {
        var pendingOrder = CreateOrderWithStatus(_user.Id, _product, OrderStatus.Shipped);
        _dbContext.Orders.Add(pendingOrder);
        await _dbContext.SaveChangesAsync();

        var handler = CreateHandler();

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new CreateReviewCommand(_product.Id, 5, "Not yet delivered"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_ThrowDomainException_When_AlreadyReviewedForCompletedOrder()
    {
        var order = CreateOrderWithStatus(_user.Id, _product, OrderStatus.Completed);
        _dbContext.Orders.Add(order);
        _dbContext.Reviews.Add(new Review(_product.Id, _user.Id, order.Id, 5, "First review"));
        await _dbContext.SaveChangesAsync();

        var handler = CreateHandler();

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new CreateReviewCommand(_product.Id, 4, "Duplicate review"), CancellationToken.None));
    }

    private static Order CreateOrderWithStatus(Guid userId, Product product, OrderStatus targetStatus)
    {
        var order = new Order(userId, $"ORD-{Guid.NewGuid():N}"[..12], "Alice", "0901234567", "123 Street");
        order.AddItem(product.Id, product.Name, product.Sku, product.Price, 1);

        if (targetStatus >= OrderStatus.Confirmed) order.UpdateStatus(OrderStatus.Confirmed);
        if (targetStatus >= OrderStatus.Processing) order.UpdateStatus(OrderStatus.Processing);
        if (targetStatus >= OrderStatus.Shipped) order.UpdateStatus(OrderStatus.Shipped);
        if (targetStatus == OrderStatus.Completed) order.UpdateStatus(OrderStatus.Completed);

        return order;
    }

    private CreateReviewHandler CreateHandler() => new(
        _reviewRepository,
        _productRepository,
        _orderRepository,
        _userRepository,
        _currentUserMock.Object,
        _dbContext,
        _mapper);

    public void Dispose() => _dbContext.Dispose();
}
