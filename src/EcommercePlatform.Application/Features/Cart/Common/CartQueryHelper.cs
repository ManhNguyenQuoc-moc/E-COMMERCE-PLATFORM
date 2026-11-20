using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Cart.DTOs;
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
        CancellationToken cancellationToken)
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
        // shortcut: discountAmount tam hardcode 0m vi chua co bang Coupons, noi logic tinh giam gia khi lam Task 5.2.
        var discountAmount = 0m;
        var totalAmount = subTotal - discountAmount;

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
