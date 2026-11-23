using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Cart.DTOs;
using EcommercePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using CartEntity = EcommercePlatform.Domain.Entities.Cart;

namespace EcommercePlatform.Application.Features.Cart.Common;

internal static class CartQueryHelper
{
    internal static Guid RequireAuthenticatedUserId(ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || !currentUser.UserGuid.HasValue || currentUser.UserGuid.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        return currentUser.UserGuid.Value;
    }

    internal static async Task<CartDto> LoadCartDtoAsync(
        IRepository<CartEntity> cartRepository,
        Guid userId,
        CancellationToken cancellationToken,
        IRepository<Coupon> couponRepository)
    {
        var cartProjection = await cartRepository
            .AsQueryable(asNoTracking: true)
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                c.Id,
                c.UserId,
                c.AppliedCouponId,
                Items = c.Items
                    .OrderBy(i => i.CreatedAt)
                    .Select(i => new CartItemDto(
                        i.Id,
                        i.ProductId,
                        i.Product!.Name,
                        i.Product.Sku,
                        i.Product.Price,
                        i.Quantity,
                        i.Product.Price * i.Quantity,
                        i.Product.StockQuantity))
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (cartProjection is null)
        {
            return CartDto.Empty(userId);
        }

        var totalItems = cartProjection.Items.Sum(i => i.Quantity);
        var subTotal = cartProjection.Items.Sum(i => i.LineTotal);
        var discountAmount = 0m;

        if (cartProjection.AppliedCouponId.HasValue && subTotal > 0m)
        {
            var coupon = await couponRepository
                .AsQueryable(asNoTracking: true)
                .FirstOrDefaultAsync(c => c.Id == cartProjection.AppliedCouponId.Value, cancellationToken);

            var now = DateTimeOffset.UtcNow;
            if (coupon is not null
                && coupon.IsActive
                && now >= coupon.StartDate
                && now <= coupon.EndDate
                && coupon.UsedCount < coupon.UsageLimit
                && subTotal >= coupon.MinOrderValue)
            {
                discountAmount = coupon.CalculateDiscount(subTotal);
            }
        }

        var totalAmount = Math.Max(0m, subTotal - discountAmount);

        return new CartDto(
            cartProjection.Id,
            cartProjection.UserId,
            cartProjection.AppliedCouponId,
            cartProjection.Items,
            totalItems,
            subTotal,
            discountAmount,
            totalAmount);
    }
}
