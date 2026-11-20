using EcommercePlatform.Application.Common.Interfaces; // Import IRepository<T>
using EcommercePlatform.Application.Features.Products.DTOs; // Import ProductDto
using EcommercePlatform.Domain.Entities; // Import Entity Product
using EcommercePlatform.Domain.Exceptions;
using Mapster;
using MapsterMapper; // Import interface IMapper
using MediatR; // Import IRequestHandler
using Microsoft.EntityFrameworkCore;

namespace EcommercePlatform.Application.Features.Products.Queries.GetProductById;

public sealed class GetProductByIdHandler(
    IRepository<Product> productRepository,
    IMapper mapper) : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await productRepository
            .AsQueryable(asNoTracking: true)
            .Where(p => p.Id == request.Id)
            .ProjectToType<ProductDto>(mapper.Config)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        return product;
    }
}