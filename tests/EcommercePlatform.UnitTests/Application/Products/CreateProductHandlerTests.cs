using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Products.Commands.CreateProduct;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EcommercePlatform.UnitTests.Application.Products;

public sealed class CreateProductHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    public CreateProductHandlerTests()
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
    public async Task Handle_Should_CreateProduct_When_CategoryExistsAndSkuIsUnique()
    {
        var category = new Category("Monitors", "monitors");
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateProductHandler(_productRepository, _categoryRepository, _unitOfWorkMock.Object);
        var id = await handler.Handle(
            new CreateProductCommand("4K Monitor", "IPS 27 inch", " mon-27 ", 450m, 12, category.Id),
            CancellationToken.None);

        var saved = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
        Assert.NotNull(saved);
        Assert.Equal("MON-27", saved.Sku);
        Assert.Equal(category.Id, saved.CategoryId);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_CategoryDoesNotExist()
    {
        var handler = new CreateProductHandler(_productRepository, _categoryRepository, _unitOfWorkMock.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new CreateProductCommand("4K Monitor", "Desc", "MON-27", 450m, 12, Guid.NewGuid()),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_ThrowDomainException_When_SkuAlreadyExists()
    {
        _dbContext.Products.Add(new Product("Existing", "Desc", "DUP-01", 100m, 5));
        await _dbContext.SaveChangesAsync();

        var handler = new CreateProductHandler(_productRepository, _categoryRepository, _unitOfWorkMock.Object);

        var exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            new CreateProductCommand("New Product", "Desc", " dup-01 ", 200m, 3),
            CancellationToken.None));

        Assert.Contains("already exists.", exception.Message);
    }

    [Fact]
    public async Task Handle_Should_ThrowDomainException_When_SkuMatchesArchivedSoftDeletedProduct()
    {
        var archived = new Product("Archived Phone", "Desc", "OLD-SKU", 500m, 0);
        archived.SoftDelete();
        _dbContext.Products.Add(archived);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateProductHandler(_productRepository, _categoryRepository, _unitOfWorkMock.Object);

        var exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            new CreateProductCommand("New Phone", "Desc", " old-sku ", 600m, 5),
            CancellationToken.None));

        Assert.Contains("soft-deleted", exception.Message);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
