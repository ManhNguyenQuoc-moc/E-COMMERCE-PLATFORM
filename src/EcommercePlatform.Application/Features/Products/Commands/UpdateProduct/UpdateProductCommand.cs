using MediatR;

namespace EcommercePlatform.Application.Features.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    Guid? CategoryId = null) : IRequest;
