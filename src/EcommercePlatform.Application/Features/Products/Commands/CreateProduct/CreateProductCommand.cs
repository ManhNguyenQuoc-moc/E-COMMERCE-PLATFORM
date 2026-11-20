using MediatR;

namespace EcommercePlatform.Application.Features.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    string Sku,
    decimal Price,
    int StockQuantity,
    Guid? CategoryId = null) : IRequest<Guid>;
