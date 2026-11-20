using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Products.Commands.PatchProduct;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EcommercePlatform.UnitTests.Application.Products;

public sealed class PatchProductHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    public PatchProductHandlerTests()
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
    public async Task Handle_Should_UpdateOnlyNonNullFields_And_PreserveExistingValues()
    {
        var category = new Category("Peripherals", "peripherals");
        var product = new Product("Mechanical Keyboard", "Cherry MX Red", "KEY-01", 120m, 15, category.Id);
        _dbContext.Categories.Add(category);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var handler = new PatchProductHandler(_productRepository, _categoryRepository, _unitOfWorkMock.Object);
        await handler.Handle(
            new PatchProductCommand(product.Id, Price: 99m),
            CancellationToken.None);

        var updated = await _dbContext.Products.FirstAsync(p => p.Id == product.Id);
        Assert.Equal(99m, updated.Price);
        Assert.Equal("Mechanical Keyboard", updated.Name);
        Assert.Equal("Cherry MX Red", updated.Description);
        Assert.Equal(category.Id, updated.CategoryId);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_ProductOrCategoryDoesNotExist()
    {
        var product = new Product("Keyboard", "Desc", "KEY-01", 120m, 15);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var handler = new PatchProductHandler(_productRepository, _categoryRepository, _unitOfWorkMock.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new PatchProductCommand(Guid.NewGuid(), Price: 50m),
            CancellationToken.None));

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new PatchProductCommand(product.Id, CategoryId: Guid.NewGuid()),
            CancellationToken.None));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
