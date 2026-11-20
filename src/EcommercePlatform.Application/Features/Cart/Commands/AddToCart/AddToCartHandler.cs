using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Cart.Common;
using EcommercePlatform.Application.Features.Cart.DTOs;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CartEntity = EcommercePlatform.Domain.Entities.Cart;

namespace EcommercePlatform.Application.Features.Cart.Commands.AddToCart;

public sealed class AddToCartHandler(
    IRepository<CartEntity> cartRepository,
    IRepository<Product> productRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<AddToCartCommand, CartDto>
{
    public async Task<CartDto> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var userId = CartQueryHelper.RequireAuthenticatedUserId(currentUser);

        var availableStock = await productRepository
            .AsQueryable(asNoTracking: true)
            .Where(p => p.Id == request.ProductId)
            .Select(p => (int?)p.StockQuantity)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        var cart = await cartRepository
            .AsQueryable(asNoTracking: false)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart is null)
        {
            cart = new CartEntity(userId);
            await cartRepository.AddAsync(cart, cancellationToken);
        }

        // shortcut: CartItem chua co RowVersion nen 2 request AddToCart song song tren cung san pham co the bi last-write-wins, bo sung RowVersion o Task 4.0 khi them concurrency token cho Product.
        cart.AddOrIncrementItem(request.ProductId, request.Quantity, availableStock);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await CartQueryHelper.LoadCartDtoAsync(cartRepository, userId, cancellationToken);
    }
}
