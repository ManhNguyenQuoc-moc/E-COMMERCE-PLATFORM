using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Cart.Common;
using EcommercePlatform.Application.Features.Reviews.DTOs;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcommercePlatform.Application.Features.Reviews.Commands.CreateReview;

public sealed class CreateReviewHandler(
    IRepository<Review> reviewRepository,
    IRepository<Product> productRepository,
    IRepository<Order> orderRepository,
    IRepository<User> userRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<CreateReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var userId = CartQueryHelper.RequireAuthenticatedUserId(currentUser);

        if (!await productRepository.AnyAsync(p => p.Id == request.ProductId, cancellationToken))
        {
            throw new NotFoundException(nameof(Product), request.ProductId);
        }

        var reviewedOrderIds = reviewRepository
            .AsQueryable(asNoTracking: true)
            .Where(r => r.UserId == userId && r.ProductId == request.ProductId)
            .Select(r => r.OrderId);

        var eligibleOrderId = await orderRepository
            .AsQueryable(asNoTracking: true)
            .Where(o => o.UserId == userId
                        && o.Status == OrderStatus.Completed
                        && o.Items.Any(i => i.ProductId == request.ProductId)
                        && !reviewedOrderIds.Contains(o.Id))
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => (Guid?)o.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!eligibleOrderId.HasValue)
        {
            var hasReviewedBefore = await reviewRepository.AnyAsync(
                r => r.UserId == userId && r.ProductId == request.ProductId,
                cancellationToken);

            throw new DomainException(hasReviewedBefore
                ? "You have already reviewed this product for your completed order(s)."
                : "You can only review products from completed orders.");
        }

        var userFullName = await userRepository
            .AsQueryable(asNoTracking: true)
            .Where(u => u.Id == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        var review = new Review(
            request.ProductId,
            userId,
            eligibleOrderId.Value,
            request.Rating,
            request.Comment);

        await reviewRepository.AddAsync(review, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<ReviewDto>((review, userFullName));
    }
}
