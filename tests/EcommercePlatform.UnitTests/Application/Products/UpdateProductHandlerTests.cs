using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Products.Commands.UpdateProduct;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EcommercePlatform.UnitTests.Application.Products;

public sealed class UpdateProductHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    public UpdateProductHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _productRepository = new Repository<Product>(_dbContext);
        _categoryRepository = new Repository<Category>(_dbContext);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct => _dbContext.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task Handle_Should_UpdateAllDetailsAndCategory_When_Valid()
    {
        var category = new Category("Accessories", "accessories");
        var product = new Product("Cable", "USB-A", "CAB-1", 10m, 50);
        _dbContext.Categories.Add(category);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateProductHandler(_productRepository, _categoryRepository, _unitOfWorkMock.Object);
        await handler.Handle(
            new UpdateProductCommand(product.Id, "USB-C Cable", "Braided 2m", 15m, category.Id),
            CancellationToken.None);

        var updated = await _dbContext.Products.FirstAsync(p => p.Id == product.Id);
        Assert.Equal("USB-C Cable", updated.Name);
        Assert.Equal("Braided 2m", updated.Description);
        Assert.Equal(15m, updated.Price);
        Assert.Equal(category.Id, updated.CategoryId);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_ProductOrCategoryMissing()
    {
        var product = new Product("Cable", "USB-A", "CAB-1", 10m, 50);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateProductHandler(_productRepository, _categoryRepository, _unitOfWorkMock.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new UpdateProductCommand(Guid.NewGuid(), "Name", "Desc", 20m),
            CancellationToken.None));

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new UpdateProductCommand(product.Id, "Name", "Desc", 20m, Guid.NewGuid()),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_UnsetCategory_When_CategoryIdIsNull()
    {
        var category = new Category("Audio", "audio");
        var product = new Product("Headphones", "Wireless", "HP-01", 80m, 10, category.Id);
        _dbContext.Categories.Add(category);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateProductHandler(_productRepository, _categoryRepository, _unitOfWorkMock.Object);
        await handler.Handle(
            new UpdateProductCommand(product.Id, "Headphones Pro", "Wireless ANC", 120m, CategoryId: null),
            CancellationToken.None);

        var updated = await _dbContext.Products.FirstAsync(p => p.Id == product.Id);
        Assert.Null(updated.CategoryId);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
