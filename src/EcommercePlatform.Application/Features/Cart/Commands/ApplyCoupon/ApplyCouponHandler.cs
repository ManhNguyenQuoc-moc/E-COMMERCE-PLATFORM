using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Cart.Common;
using EcommercePlatform.Application.Features.Cart.DTOs;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CartEntity = EcommercePlatform.Domain.Entities.Cart;

namespace EcommercePlatform.Application.Features.Cart.Commands.ApplyCoupon;

public sealed class ApplyCouponHandler(
    IRepository<CartEntity> cartRepository,
    IRepository<Coupon> couponRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IRepository<User>? userRepository = null) : IRequestHandler<ApplyCouponCommand, CartDto>
{
    public async Task<CartDto> Handle(ApplyCouponCommand request, CancellationToken cancellationToken)
    {
        var userId = CartQueryHelper.RequireAuthenticatedUserId(currentUser);

        var cart = await cartRepository
            .AsQueryable(asNoTracking: false)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart is null || cart.Items.Count == 0)
        {
            throw new DomainException("Your cart is empty.");
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var coupon = await couponRepository
            .AsQueryable(asNoTracking: true)
            .FirstOrDefaultAsync(c => c.Code == normalizedCode, cancellationToken)
            ?? throw new NotFoundException(nameof(Coupon), normalizedCode);

        var subTotal = cart.Items.Sum(i => i.Product!.Price * i.Quantity);
        coupon.ValidateCanApply(subTotal, DateTimeOffset.UtcNow);

        cart.ApplyCoupon(coupon.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await CartQueryHelper.LoadCartDtoAsync(cartRepository, userId, cancellationToken, couponRepository, userRepository);
    }
}
