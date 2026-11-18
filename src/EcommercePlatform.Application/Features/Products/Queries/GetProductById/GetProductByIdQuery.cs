using EcommercePlatform.Application.Features.Products.DTOs;
using MediatR;

namespace EcommercePlatform.Application.Features.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;

