using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Cart.Common;
using EcommercePlatform.Application.Features.Cart.DTOs;
using EcommercePlatform.Domain.Entities;
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

            var availableStock = await productRepository
                .AsQueryable(asNoTracking: true)
                .Where(p => p.Id == item.ProductId)
                .Select(p => (int?)p.StockQuantity)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException(nameof(Product), item.ProductId);

            cart.UpdateItemQuantity(request.CartItemId, request.Quantity, availableStock);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await CartQueryHelper.LoadCartDtoAsync(cartRepository, userId, cancellationToken, couponRepository, userRepository);
    }
}
