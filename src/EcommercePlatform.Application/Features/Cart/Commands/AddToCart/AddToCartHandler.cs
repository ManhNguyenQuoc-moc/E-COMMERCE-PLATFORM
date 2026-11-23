using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Cart.Common;
using EcommercePlatform.Application.Features.Cart.DTOs;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CartEntity = EcommercePlatform.Domain.Entities.Cart;

namespace EcommercePlatform.Application.Features.Cart.Commands.AddToCart;

public sealed class AddToCartHandler(
    IRepository<CartEntity> cartRepository,
    IRepository<Product> productRepository,
    IRepository<Coupon> couponRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IRepository<User>? userRepository = null) : IRequestHandler<AddToCartCommand, CartDto>
{
    public async Task<CartDto> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var userId = CartQueryHelper.RequireAuthenticatedUserId(currentUser);

        var productInfo = await productRepository
            .AsQueryable(asNoTracking: true)
            .Where(p => p.Id == request.ProductId)
            .Select(p => new { p.Name, p.StockQuantity, p.Status })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        if (productInfo.Status is ProductStatus.Draft or ProductStatus.Inactive)
        {
            throw new DomainException($"Product '{productInfo.Name}' is not available for purchase.");
        }

        var cart = await cartRepository
            .AsQueryable(asNoTracking: false)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart is null)
        {
            cart = new CartEntity(userId);
            await cartRepository.AddAsync(cart, cancellationToken);
        }

        cart.AddOrIncrementItem(request.ProductId, request.Quantity, productInfo.StockQuantity);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await CartQueryHelper.LoadCartDtoAsync(cartRepository, userId, cancellationToken, couponRepository, userRepository);
    }
}
