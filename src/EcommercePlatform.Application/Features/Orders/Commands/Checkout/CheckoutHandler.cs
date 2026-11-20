using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Cart.Common;
using EcommercePlatform.Application.Features.Orders.DTOs;
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
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<CheckoutCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        var userId = CartQueryHelper.RequireAuthenticatedUserId(currentUser);

        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var userRank = await userRepository
                .AsQueryable(asNoTracking: true)
                .Where(u => u.Id == userId)
                .Select(u => (Domain.Enums.UserRank?)u.Rank)
                .FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException(nameof(User), userId);

            var cart = await cartRepository
                .AsQueryable(asNoTracking: false)
                .IgnoreQueryFilters()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId, ct);

            if (cart is null || cart.Items.Count == 0)
            {
                throw new DomainException("Your cart is empty.");
            }

            var checkoutItems = cart.Items.OrderBy(i => i.ProductId).ToList();
            var productIds = checkoutItems.Select(i => i.ProductId).Distinct().ToList();

            var productsById = await productRepository
                .AsQueryable(asNoTracking: false)
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

            foreach (var item in checkoutItems)
            {
                if (!productsById.TryGetValue(item.ProductId, out var product))
                {
                    throw new DomainException("One or more products in your cart are no longer available.");
                }

                // Deduct stock on the tracked Product entity protected by SQL Server RowVersion concurrency token.
                product.AdjustStock(-item.Quantity);
                order.AddItem(product.Id, product.Name, product.Sku, product.Price, item.Quantity);
            }

            // TODO(Task 5.2): Replace hardcoded 0m with coupon discount calculation once Coupons are implemented.
            order.ApplyDiscounts(userRank, couponDiscountAmount: 0m);

            await orderRepository.AddAsync(order, ct);
            cart.RemoveItems(checkoutItems.Select(i => i.Id));

            return mapper.Map<OrderDto>(order);
        }, cancellationToken);
    }
}
