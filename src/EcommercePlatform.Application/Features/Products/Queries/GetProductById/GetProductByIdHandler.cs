using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Products.DTOs;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using Mapster;
using MapsterMapper;
using MediatR;
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