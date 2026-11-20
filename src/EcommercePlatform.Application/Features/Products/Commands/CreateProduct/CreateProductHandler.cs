using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using MapsterMapper;
using MediatR;

namespace EcommercePlatform.Application.Features.Products.Commands.CreateProduct;

public sealed class CreateProductHandler(
    IRepository<Product> productRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var normalizedSku = request.Sku.Trim().ToUpperInvariant();
        if (await productRepository.AnyAsync(p => p.Sku == normalizedSku, cancellationToken))
        {
            throw new DomainException($"Product with SKU '{normalizedSku}' already exists.");
        }

        var product = mapper.Map<Product>(request);

        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}

