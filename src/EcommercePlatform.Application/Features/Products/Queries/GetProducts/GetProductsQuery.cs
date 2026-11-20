using EcommercePlatform.Application.Common.Models;
using EcommercePlatform.Application.Features.Products.DTOs;
using MediatR;

namespace EcommercePlatform.Application.Features.Products.Queries.GetProducts;

public sealed record GetProductsQuery(
    int Page = 1,
    int Size = 10,
    string? Search = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null) : IRequest<PaginatedList<ProductDto>>;
