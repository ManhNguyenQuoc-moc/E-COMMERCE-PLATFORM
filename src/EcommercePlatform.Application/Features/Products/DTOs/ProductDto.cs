using EcommercePlatform.Domain.Enums;

namespace EcommercePlatform.Application.Features.Products.DTOs;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Description,
    string Sku,
    decimal Price,
    int StockQuantity,
    ProductStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

