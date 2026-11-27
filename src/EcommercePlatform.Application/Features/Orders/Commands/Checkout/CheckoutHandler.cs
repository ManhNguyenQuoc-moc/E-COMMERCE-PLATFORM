using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Cart.Common;
using EcommercePlatform.Application.Features.Orders.DTOs;
using EcommercePlatform.Application.Features.Orders.EventHandlers;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CartEntity = EcommercePlatform.Domain.Entities.Cart;

namespace EcommercePlatform.Application.Features.Orders.Commands.Checkout;

public sealed class CheckoutHandler(
    IRepository<CartEntity> cartRepository,
    IRepository<Product> productRepository,
    IRepository<User> userRepository,
    IRepository<Order> orderRepository,
    IRepository<Coupon> couponRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IPublisher? publisher = null) : IRequestHandler<CheckoutCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        var userId = CartQueryHelper.RequireAuthenticatedUserId(currentUser);
        List<Guid> purchasedProductIds = [];

        var orderDto = await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var cart = await cartRepository
                .AsQueryable(asNoTracking: false)
                .IgnoreQueryFilters()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId, ct);

            if (cart is null || cart.Items.Count == 0)
            {
                throw new DomainException("Your cart is empty.");
            }

            var userRank = await userRepository
                .AsQueryable(asNoTracking: true)
                .Where(u => u.Id == userId)
                .Select(u => (Domain.Enums.UserRank?)u.Rank)
                .FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException(nameof(User), userId);

            var allCartItems = cart.Items.OrderBy(i => i.ProductId).ToList();
            var productIds = allCartItems.Select(i => i.ProductId).Distinct().ToList();
            purchasedProductIds = productIds;

            var productsById = await productRepository
                .AsQueryable(asNoTracking: false)
                .IgnoreQueryFilters()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct);

            var orderNumber = $"ORD-{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            var order = new Order(
                userId,
                orderNumber,
                request.ShippingRecipientName,
                request.ShippingPhone,
                request.ShippingAddress,
                request.Notes,
                cart.AppliedCouponId);

            foreach (var item in allCartItems)
            {
                if (!productsById.TryGetValue(item.ProductId, out var product))
                {
                    throw new DomainException($"Product with ID '{item.ProductId}' is no longer available. Please remove it from your cart before checking out.");
                }

                if (product.IsDeleted)
                {
                    throw new DomainException($"Product '{product.Name}' (SKU: {product.Sku}) has been removed and is no longer available. Please remove it from your cart before checking out.");
                }

                if (product.Status is Domain.Enums.ProductStatus.Draft or Domain.Enums.ProductStatus.Inactive)
                {
                    throw new DomainException($"Product '{product.Name}' is not available for purchase. Please remove it from your cart before checking out.");
                }

                product.AdjustStock(-item.Quantity);
                order.AddItem(product.Id, product.Name, product.Sku, product.Price, item.Quantity);
            }

            var couponDiscountAmount = 0m;
            if (cart.AppliedCouponId.HasValue)
            {
                var coupon = await couponRepository
                    .AsQueryable(asNoTracking: false)
                    .FirstOrDefaultAsync(c => c.Id == cart.AppliedCouponId.Value, ct)
                    ?? throw new DomainException("Applied coupon is no longer available.");

                coupon.ValidateCanApply(order.SubTotal, DateTimeOffset.UtcNow);
                couponDiscountAmount = coupon.CalculateDiscount(order.SubTotal);
                coupon.IncrementUsage();
            }

            order.ApplyDiscounts(userRank, couponDiscountAmount);

            await orderRepository.AddAsync(order, ct);
            cart.RemoveItems(allCartItems.Select(i => i.Id));

            return mapper.Map<OrderDto>(order);
        }, cancellationToken);

        // Phát sự kiện MediatR.Publish SAU KHI Transaction SQL Server đã Commit thành công:
        // -> Kích hoạt cả OrderPlacedCacheInvalidationHandler (Redis) lẫn OrderPlacedRealtimeHandler (SignalR)
        if (publisher is not null)
        {
            await publisher.Publish(
                new OrderPlacedNotification(
                    orderDto.Id,
                    userId,
                    orderDto.OrderNumber,
                    orderDto.TotalAmount,
                    orderDto.ShippingRecipientName,
                    purchasedProductIds),
                cancellationToken);
        }

        return orderDto;
    }
}