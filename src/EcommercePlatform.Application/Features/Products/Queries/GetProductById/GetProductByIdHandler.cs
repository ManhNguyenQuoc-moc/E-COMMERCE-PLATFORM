using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Products.DTOs;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcommercePlatform.Application.Features.Products.Queries.GetProductById;

public sealed class GetProductByIdHandler(IAppDbContext dbContext)
    : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Description,
                p.Sku,
                p.Price,
                p.StockQuantity,
                p.Status,
                p.CreatedAt,
                p.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return product ?? throw new NotFoundException(nameof(Product), request.Id);
    }
}

