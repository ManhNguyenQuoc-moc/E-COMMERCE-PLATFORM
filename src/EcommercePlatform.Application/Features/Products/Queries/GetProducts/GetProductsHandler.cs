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
    IMapper mapper) : IRequestHandler<GetProductsQuery, PaginatedList<ProductDto>>
{
    public async Task<PaginatedList<ProductDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.Size < 1 ? 10 : Math.Min(request.Size, 100);

        var query = productRepository.AsQueryable(asNoTracking: true);

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
                p.Sku.Contains(keyword) ||
                p.Description.Contains(keyword));
        }

        var projected = query
            .OrderByDescending(p => p.CreatedAt)
            .ProjectToType<ProductDto>(mapper.Config);

        return await PaginatedList<ProductDto>.CreateAsync(projected, page, size, cancellationToken);
    }
}
