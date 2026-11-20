using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Products.Commands.DeleteProduct;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EcommercePlatform.UnitTests.Application.Products;

public sealed class DeleteProductHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<Product> _productRepository;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    public DeleteProductHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _productRepository = new Repository<Product>(_dbContext);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct => _dbContext.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task Handle_Should_SoftDeleteProduct_WithoutRemovingRecordFromDatabase()
    {
        var product = new Product("Webcam", "1080p", "CAM-01", 80m, 10);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteProductHandler(_productRepository, _unitOfWorkMock.Object);
        await handler.Handle(new DeleteProductCommand(product.Id), CancellationToken.None);

        Assert.Null(await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == product.Id));

        var rawRecord = await _dbContext.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == product.Id);
        Assert.NotNull(rawRecord);
        Assert.True(rawRecord.IsDeleted);
        Assert.NotNull(rawRecord.DeletedAt);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_ProductMissingOrAlreadySoftDeleted()
    {
        var deleted = new Product("Old Cam", "720p", "CAM-00", 30m, 0);
        deleted.SoftDelete();
        _dbContext.Products.Add(deleted);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteProductHandler(_productRepository, _unitOfWorkMock.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new DeleteProductCommand(Guid.NewGuid()), CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new DeleteProductCommand(deleted.Id), CancellationToken.None));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
