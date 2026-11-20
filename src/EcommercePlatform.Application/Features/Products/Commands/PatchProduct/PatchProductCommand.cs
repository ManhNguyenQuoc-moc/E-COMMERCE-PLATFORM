using MediatR;

namespace EcommercePlatform.Application.Features.Products.Commands.PatchProduct;

public sealed record PatchProductCommand(
    Guid Id,
    string? Name = null,
    string? Description = null,
    decimal? Price = null,
    Guid? CategoryId = null) : IRequest;
