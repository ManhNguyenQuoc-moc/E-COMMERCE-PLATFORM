using EcommercePlatform.Application.Features.Cart.DTOs;
using MediatR;

namespace EcommercePlatform.Application.Features.Cart.Queries.GetCart;

public sealed record GetCartQuery : IRequest<CartDto>;
