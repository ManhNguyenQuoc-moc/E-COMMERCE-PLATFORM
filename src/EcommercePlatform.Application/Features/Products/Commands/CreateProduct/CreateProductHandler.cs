using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Domain.Entities;
using MediatR;

namespace EcommercePlatform.Application.Features.Products.Commands.CreateProduct;

public sealed class CreateProductHandler(IAppDbContext dbContext)
    : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product(
            request.Name,
            request.Description,
            request.Sku,
            request.Price,
            request.StockQuantity);

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}

