using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Common.Mappings;
using EcommercePlatform.Application.Features.Products.Queries.GetProductById;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace EcommercePlatform.UnitTests.Application.Products;

public sealed class GetProductByIdHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<Product> _productRepository;
    private readonly IMapper _mapper;

    public GetProductByIdHandlerTests()
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
    public async Task Handle_Should_ReturnProductDtoWithCategoryName_When_Found()
    {
        var category = new Category("Audio", "audio");
        var product = new Product("Headphones", "Noise cancelling", "AUD-01", 300m, 8, category.Id);
        _dbContext.Categories.Add(category);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var handler = new GetProductByIdHandler(_productRepository, _mapper);
        var dto = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        Assert.Equal(product.Id, dto.Id);
        Assert.Equal(category.Id, dto.CategoryId);
        Assert.Equal("Audio", dto.CategoryName);
        Assert.Equal("Headphones", dto.Name);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_MissingOrSoftDeleted()
    {
        var deleted = new Product("Deleted", "Desc", "DEL-01", 10m, 1);
        deleted.SoftDelete();
        _dbContext.Products.Add(deleted);
        await _dbContext.SaveChangesAsync();

        var handler = new GetProductByIdHandler(_productRepository, _mapper);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetProductByIdQuery(Guid.NewGuid()), CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetProductByIdQuery(deleted.Id), CancellationToken.None));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
