using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Cart.Common;
using EcommercePlatform.Application.Features.Cart.DTOs;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
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
            .IgnoreQueryFilters()
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart is null || cart.Items.Count == 0)
        {
            throw new DomainException("Your cart is empty.");
        }

        var deletedItem = cart.Items.FirstOrDefault(i => i.Product is null || i.Product.IsDeleted);
        if (deletedItem is not null)
        {
            var productName = deletedItem.Product?.Name ?? deletedItem.ProductId.ToString();
            throw new DomainException($"Product '{productName}' has been removed and is no longer available. Please remove it from your cart before applying a coupon.");
        }

        var inactiveItem = cart.Items.FirstOrDefault(i => i.Product!.Status is ProductStatus.Draft or ProductStatus.Inactive);
        if (inactiveItem is not null)
        {
            throw new DomainException($"Product '{inactiveItem.Product!.Name}' is not available for purchase. Please remove it from your cart before applying a coupon.");
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
