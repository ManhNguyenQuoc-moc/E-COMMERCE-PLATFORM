using EcommercePlatform.Application.Features.Cart.DTOs;
using MediatR;

namespace EcommercePlatform.Application.Features.Cart.Commands.AddToCart;

public sealed record AddToCartCommand(Guid ProductId, int Quantity) : IRequest<CartDto>;
