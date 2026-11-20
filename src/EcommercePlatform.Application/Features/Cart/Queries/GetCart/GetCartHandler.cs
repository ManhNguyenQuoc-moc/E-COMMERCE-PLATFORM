using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Cart.Common;
using EcommercePlatform.Application.Features.Cart.DTOs;
using MediatR;
using CartEntity = EcommercePlatform.Domain.Entities.Cart;

namespace EcommercePlatform.Application.Features.Cart.Queries.GetCart;

public sealed class GetCartHandler(
    IRepository<CartEntity> cartRepository,
    ICurrentUser currentUser) : IRequestHandler<GetCartQuery, CartDto>
{
    public async Task<CartDto> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var userId = CartQueryHelper.RequireAuthenticatedUserId(currentUser);
        return await CartQueryHelper.LoadCartDtoAsync(cartRepository, userId, cancellationToken);
    }
}
