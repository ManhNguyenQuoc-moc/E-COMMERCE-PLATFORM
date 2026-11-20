using EcommercePlatform.Application.Common.Interfaces; // Import IRepository<T>
using EcommercePlatform.Application.Common.Models; // Import PaginatedList<T>
using EcommercePlatform.Application.Features.Products.DTOs; // Import ProductDto
using EcommercePlatform.Domain.Entities; // Import Entity Product
using Mapster;
using MapsterMapper; // Import interface IMapper
using MediatR; // Import IRequestHandler

namespace EcommercePlatform.Application.Features.Products.Queries.GetProducts;

public sealed class GetProductsHandler(
    IRepository<Product> productRepository,
    IMapper mapper) : IRequestHandler<GetProductsQuery, PaginatedList<ProductDto>>
{
    public async Task<PaginatedList<ProductDto>> Handle(
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
                p.Sku.Contains(keyword) ||
                p.Description.Contains(keyword));
        }

        var projected = query
            .OrderByDescending(p => p.CreatedAt)
            .ProjectToType<ProductDto>(mapper.Config);

        return await PaginatedList<ProductDto>.CreateAsync(projected, request.Page, request.Size, cancellationToken);
    }
}