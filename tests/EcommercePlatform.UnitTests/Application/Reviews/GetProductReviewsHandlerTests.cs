using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Common.Mappings;
using EcommercePlatform.Application.Features.Reviews.Queries.GetProductReviews;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace EcommercePlatform.UnitTests.Application.Reviews;

public sealed class GetProductReviewsHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<Review> _reviewRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IMapper _mapper;
    private readonly Product _product;

    public GetProductReviewsHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _reviewRepository = new Repository<Review>(_dbContext);
        _productRepository = new Repository<Product>(_dbContext);

        _product = new Product("Sunscreen", "50ml", "SUN-01", 300_000m, 20);
        _dbContext.Products.Add(_product);
        _dbContext.SaveChanges();

        var config = new TypeAdapterConfig();
        new ReviewMapProfile().Register(config);
        _mapper = new Mapper(config);
    }

    [Fact]
    public async Task Handle_Should_ReturnPaginatedReviewsAndAverageRating()
    {
        var user1 = new User(Guid.NewGuid(), "Alice");
        var user2 = new User(Guid.NewGuid(), "Bob");
        var user3 = new User(Guid.NewGuid(), "Charlie");
        _dbContext.Users.AddRange(user1, user2, user3);

        _dbContext.Reviews.AddRange(
            new Review(_product.Id, user1.Id, Guid.NewGuid(), 5, "Perfect"),
            new Review(_product.Id, user2.Id, Guid.NewGuid(), 4, "Good"),
            new Review(_product.Id, user3.Id, Guid.NewGuid(), 4, "Nice"));
        await _dbContext.SaveChangesAsync();

        var handler = new GetProductReviewsHandler(_reviewRepository, _productRepository, _mapper);
        var result = await handler.Handle(new GetProductReviewsQuery(_product.Id, Page: 1, Size: 2), CancellationToken.None);

        Assert.Equal(_product.Id, result.ProductId);
        Assert.Equal(3, result.TotalReviews);
        Assert.Equal(4.33m, result.AverageRating);
        Assert.Equal(2, result.Reviews.Items.Count);
        Assert.Equal(3, result.Reviews.TotalCount);
        Assert.True(result.Reviews.HasNextPage);
    }

    [Fact]
    public async Task Handle_Should_ReturnZeroSummary_When_ProductHasNoReviews()
    {
        var handler = new GetProductReviewsHandler(_reviewRepository, _productRepository, _mapper);
        var result = await handler.Handle(new GetProductReviewsQuery(_product.Id), CancellationToken.None);

        Assert.Equal(0, result.TotalReviews);
        Assert.Equal(0m, result.AverageRating);
        Assert.Empty(result.Reviews.Items);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_ProductDoesNotExist()
    {
        var handler = new GetProductReviewsHandler(_reviewRepository, _productRepository, _mapper);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetProductReviewsQuery(Guid.NewGuid()), CancellationToken.None));
    }

    public void Dispose() => _dbContext.Dispose();
}
