using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Cart.Common;
using EcommercePlatform.Application.Features.Cart.DTOs;
using EcommercePlatform.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CartEntity = EcommercePlatform.Domain.Entities.Cart;

namespace EcommercePlatform.Application.Features.Cart.Commands.RemoveCoupon;

public sealed class RemoveCouponHandler(
    IRepository<CartEntity> cartRepository,
    IRepository<Coupon> couponRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<RemoveCouponCommand, CartDto>
{
    public async Task<CartDto> Handle(RemoveCouponCommand request, CancellationToken cancellationToken)
    {
        var userId = CartQueryHelper.RequireAuthenticatedUserId(currentUser);

        var cart = await cartRepository
            .AsQueryable(asNoTracking: false)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart is not null && cart.AppliedCouponId.HasValue)
        {
            cart.ClearCoupon();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return await CartQueryHelper.LoadCartDtoAsync(cartRepository, userId, cancellationToken, couponRepository);
    }
}
