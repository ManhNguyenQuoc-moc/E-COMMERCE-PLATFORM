using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Cart.DTOs;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
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
        IRepository<Coupon> couponRepository,
        IRepository<User>? userRepository = null)
    {
        var cartProjection = await cartRepository
            .AsQueryable(asNoTracking: true)
            .IgnoreQueryFilters()
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
                        i.Product.IsDeleted ? 0 : i.Product.StockQuantity,
                        !i.Product.IsDeleted && i.Product.Status != ProductStatus.Draft && i.Product.Status != ProductStatus.Inactive))
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (cartProjection is null)
        {
            return CartDto.Empty(userId);
        }

        var totalItems = cartProjection.Items.Sum(i => i.Quantity);
        var subTotal = cartProjection.Items.Where(i => i.IsAvailable).Sum(i => i.LineTotal);
        var couponDiscountAmount = 0m;
        var rankDiscountAmount = 0m;

        if (subTotal > 0m)
        {
            if (userRepository is not null)
            {
                var userRank = await userRepository
                    .AsQueryable(asNoTracking: true)
                    .Where(u => u.Id == userId)
                    .Select(u => (UserRank?)u.Rank)
                    .FirstOrDefaultAsync(cancellationToken) ?? UserRank.Member;

                rankDiscountAmount = Order.CalculateRankDiscount(userRank, subTotal);
            }

            if (cartProjection.AppliedCouponId.HasValue)
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
                    var remainingAfterRank = Math.Max(0m, subTotal - rankDiscountAmount);
                    couponDiscountAmount = Math.Min(coupon.CalculateDiscount(subTotal), remainingAfterRank);
                }
            }
        }

        var discountAmount = rankDiscountAmount + couponDiscountAmount;
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
