using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Products.DTOs;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using MapsterMapper;
using MediatR;

namespace EcommercePlatform.Application.Features.Products.Queries.GetProductById;

public sealed class GetProductByIdHandler(
    IRepository<Product> productRepository,
    IMapper mapper) : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, asNoTracking: true, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        return mapper.Map<ProductDto>(product);
    }
}

