using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Cart.Common;
using EcommercePlatform.Application.Features.Cart.DTOs;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CartEntity = EcommercePlatform.Domain.Entities.Cart;

namespace EcommercePlatform.Application.Features.Cart.Commands.UpdateCartItem;

public sealed class UpdateCartItemHandler(
    IRepository<CartEntity> cartRepository,
    IRepository<CartItem> cartItemRepository,
    IRepository<Product> productRepository,
    IRepository<Coupon> couponRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IRepository<User>? userRepository = null) : IRequestHandler<UpdateCartItemCommand, CartDto>
{
    public async Task<CartDto> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
    {
        var userId = CartQueryHelper.RequireAuthenticatedUserId(currentUser);

        var cart = await cartRepository
            .AsQueryable(asNoTracking: false)
            .IgnoreQueryFilters()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(CartItem), request.CartItemId);

        if (request.Quantity == 0)
        {
            var removedItem = cart.RemoveItem(request.CartItemId);
            cartItemRepository.Remove(removedItem);
        }
        else
        {
            var item = cart.Items.FirstOrDefault(i => i.Id == request.CartItemId)
                ?? throw new NotFoundException(nameof(CartItem), request.CartItemId);

            var productInfo = await productRepository
                .AsQueryable(asNoTracking: true)
                .IgnoreQueryFilters()
                .Where(p => p.Id == item.ProductId)
                .Select(p => new { p.Name, p.StockQuantity, p.Status, p.IsDeleted })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException(nameof(Product), item.ProductId);

            if (productInfo.IsDeleted)
            {
                throw new DomainException($"Product '{productInfo.Name}' has been removed and is no longer available. Please remove it from your cart.");
            }

            if (productInfo.Status is ProductStatus.Draft or ProductStatus.Inactive)
            {
                throw new DomainException($"Product '{productInfo.Name}' is not available for purchase.");
            }

            cart.UpdateItemQuantity(request.CartItemId, request.Quantity, productInfo.StockQuantity);
        }

        if (cart.AppliedCouponId.HasValue)
        {
            var coupon = await couponRepository
                .AsQueryable(asNoTracking: true)
                .FirstOrDefaultAsync(c => c.Id == cart.AppliedCouponId.Value, cancellationToken)
                ?? throw new DomainException("Applied coupon is no longer available. Please remove the coupon from your cart.");

            var remainingProductIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
            var activeProductPrices = remainingProductIds.Count == 0
                ? new Dictionary<Guid, decimal>()
                : await productRepository
                    .AsQueryable(asNoTracking: true)
                    .IgnoreQueryFilters()
                    .Where(p => remainingProductIds.Contains(p.Id)
                                && !p.IsDeleted
                                && p.Status != ProductStatus.Draft
                                && p.Status != ProductStatus.Inactive)
                    .ToDictionaryAsync(p => p.Id, p => p.Price, cancellationToken);

            var updatedSubTotal = cart.Items
                .Where(i => activeProductPrices.ContainsKey(i.ProductId))
                .Sum(i => activeProductPrices[i.ProductId] * i.Quantity);

            coupon.ValidateCanApply(updatedSubTotal, DateTimeOffset.UtcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await CartQueryHelper.LoadCartDtoAsync(cartRepository, userId, cancellationToken, couponRepository, userRepository);
    }
}
