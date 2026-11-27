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
    IMapper mapper,
    ICacheService? cacheService = null)
    : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"products:by-id:{request.Id}";

        // Nếu không truyền cacheService (như trong các Unit Test cũ), query thẳng xuống SQL Server
        if (cacheService is null)
        {
            return await QueryProductByIdAsync(request.Id, cancellationToken);
        }

        // Nếu có cacheService -> Sử dụng GetOrCreateAsync (có khóa SemaphoreSlim chống Cache Miss đồng loạt)
        return await cacheService.GetOrCreateAsync(
            cacheKey,
            ct => QueryProductByIdAsync(request.Id, ct),
            TimeSpan.FromMinutes(5),
            cancellationToken);
    }

    private async Task<ProductDto> QueryProductByIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        return await productRepository
            .AsQueryable(asNoTracking: true)
            .Where(p => p.Id == productId)
            .ProjectToType<ProductDto>(mapper.Config)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Product), productId);
    }
}