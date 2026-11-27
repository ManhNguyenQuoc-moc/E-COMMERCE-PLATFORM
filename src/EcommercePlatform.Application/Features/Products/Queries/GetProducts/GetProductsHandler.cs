using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Common.Models;
using EcommercePlatform.Application.Features.Products.DTOs;
using EcommercePlatform.Domain.Entities;
using Mapster;
using MapsterMapper;
using MediatR;

namespace EcommercePlatform.Application.Features.Products.Queries.GetProducts;

public sealed class GetProductsHandler(
    IRepository<Product> productRepository,
    IMapper mapper, ICacheService? cacheService = null) : IRequestHandler<GetProductsQuery, PaginatedList<ProductDto>>
{
    public async Task<PaginatedList<ProductDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        // Nếu không truyền cacheService (như trong các Unit Test cũ), query thẳng xuống SQL Server
        if (cacheService is null)
        {
            return await QueryProductsFromDatabaseAsync(request, cancellationToken);
        }

        // Lấy version hiện tại của danh sách sản phẩm và gọi GetOrCreateAsync (có khóa SemaphoreSlim chống Cache Stampede)
        var version = await cacheService.GetVersionAsync("products:version", cancellationToken);
        var cacheKey = $"products:list:v{version}:p{request.Page}:s{request.Size}:c{request.CategoryId}:min{request.MinPrice}:max{request.MaxPrice}:q{request.Search?.Trim().ToLowerInvariant()}";

        return await cacheService.GetOrCreateAsync(
            cacheKey,
            ct => QueryProductsFromDatabaseAsync(request, ct),
            TimeSpan.FromMinutes(5),
            cancellationToken);
    }

    private async Task<PaginatedList<ProductDto>> QueryProductsFromDatabaseAsync(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var query = productRepository.AsQueryable(asNoTracking: true);

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= request.MaxPrice.Value);
        }

        var keyword = request.Search?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            query = query.Where(p =>
                p.Name.Contains(keyword) ||
                p.Sku.Contains(keyword));
        }

        var projected = query
            .OrderByDescending(p => p.CreatedAt)
            .ProjectToType<ProductDto>(mapper.Config);

        return await PaginatedList<ProductDto>.CreateAsync(projected, request.Page, request.Size, cancellationToken);
    }
}