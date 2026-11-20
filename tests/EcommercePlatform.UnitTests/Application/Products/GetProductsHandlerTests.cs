using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Common.Mappings;
using EcommercePlatform.Application.Features.Products.Queries.GetProducts;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace EcommercePlatform.UnitTests.Application.Products;

public sealed class GetProductsHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<Product> _productRepository;
    private readonly IMapper _mapper;

    public GetProductsHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _productRepository = new Repository<Product>(_dbContext);

        var config = new TypeAdapterConfig();
        new ProductMapProfile().Register(config);
        _mapper = new Mapper(config);
    }

    [Fact]
    public async Task Handle_Should_FilterByCategoryPriceSearch_And_ExcludeSoftDeleted()
    {
        var electronics = new Category("Electronics", "electronics");
        var books = new Category("Books", "books");
        var laptop = new Product("Gaming Laptop", "RTX GPU", "LAP-001", 1500m, 5, electronics.Id);
        var mouse = new Product("Gaming Mouse", "Wireless", "MOU-001", 50m, 20, electronics.Id);
        var novel = new Product("Sci-Fi Novel", "Paperback", "BOK-001", 25m, 10, books.Id);
        var deletedLaptop = new Product("Old Laptop", "Discontinued", "LAP-OLD", 1200m, 2, electronics.Id);
        deletedLaptop.SoftDelete();

        _dbContext.Categories.AddRange(electronics, books);
        _dbContext.Products.AddRange(laptop, mouse, novel, deletedLaptop);
        await _dbContext.SaveChangesAsync();

        var handler = new GetProductsHandler(_productRepository, _mapper);
        var result = await handler.Handle(
            new GetProductsQuery(Page: 1, Size: 10, CategoryId: electronics.Id, Search: "Gaming", MinPrice: 100m, MaxPrice: 2000m),
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(laptop.Id, item.Id);
        Assert.Equal("Electronics", item.CategoryName);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Handle_Should_PaginateResultsCorrectly()
    {
        for (var i = 1; i <= 5; i++)
        {
            _dbContext.Products.Add(new Product($"Product {i}", "Desc", $"SKU-{i}", i * 10m, 5));
        }

        await _dbContext.SaveChangesAsync();

        var handler = new GetProductsHandler(_productRepository, _mapper);
        var page2 = await handler.Handle(new GetProductsQuery(Page: 2, Size: 2), CancellationToken.None);

        Assert.Equal(2, page2.Items.Count);
        Assert.Equal(5, page2.TotalCount);
        Assert.Equal(3, page2.TotalPages);
        Assert.True(page2.HasPreviousPage);
        Assert.True(page2.HasNextPage);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
