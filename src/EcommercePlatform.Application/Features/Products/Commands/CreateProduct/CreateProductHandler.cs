using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcommercePlatform.Application.Features.Products.Commands.CreateProduct;

public sealed class CreateProductHandler(
    IRepository<Product> productRepository,
    IRepository<Category> categoryRepository,
    IUnitOfWork unitOfWork,
    ICacheService? cacheService = null) : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (request.CategoryId.HasValue)
        {
            var categoryExists = await categoryRepository.AnyAsync(
                c => c.Id == request.CategoryId.Value,
                cancellationToken);

            if (!categoryExists)
            {
                throw new NotFoundException(nameof(Category), request.CategoryId.Value);
            }
        }

        var normalizedSku = request.Sku.Trim().ToUpperInvariant();
        var existingSku = await productRepository
            .AsQueryable(asNoTracking: true)
            .IgnoreQueryFilters()
            .Where(p => p.Sku == normalizedSku)
            .Select(p => new { p.IsDeleted })
            .FirstOrDefaultAsync(cancellationToken);

        if (existingSku is not null)
        {
            var message = existingSku.IsDeleted
                ? $"Product with SKU '{normalizedSku}' already exists in archived products (soft-deleted) and cannot be reused."
                : $"Product with SKU '{normalizedSku}' already exists.";

            throw new DomainException(message);
        }

        var product = new Product(
            request.Name,
            request.Description,
            request.Sku,
            request.Price,
            request.StockQuantity,
            request.CategoryId);

        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (cacheService is not null)
        {
            await cacheService.IncrementVersionAsync("products:version", cancellationToken);
        }

        return product.Id;
    }
}
