using EcommercePlatform.Application.Features.Cart.DTOs;
using MediatR;

namespace EcommercePlatform.Application.Features.Cart.Commands.UpdateCartItem;

public sealed record UpdateCartItemCommand(Guid CartItemId, int Quantity) : IRequest<CartDto>;
