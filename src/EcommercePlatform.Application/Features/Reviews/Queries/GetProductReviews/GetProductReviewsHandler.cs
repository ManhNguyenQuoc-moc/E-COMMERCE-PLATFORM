using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Common.Models;
using EcommercePlatform.Application.Features.Reviews.DTOs;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using Mapster;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcommercePlatform.Application.Features.Reviews.Queries.GetProductReviews;

public sealed class GetProductReviewsHandler(
    IRepository<Review> reviewRepository,
    IRepository<Product> productRepository,
    IMapper mapper) : IRequestHandler<GetProductReviewsQuery, ProductReviewsSummaryDto>
{
    public async Task<ProductReviewsSummaryDto> Handle(
        GetProductReviewsQuery request,
        CancellationToken cancellationToken)
    {
        if (!await productRepository.AnyAsync(p => p.Id == request.ProductId, cancellationToken))
        {
            throw new NotFoundException(nameof(Product), request.ProductId);
        }

        var summary = await reviewRepository
            .AsQueryable(asNoTracking: true)
            .Where(r => r.ProductId == request.ProductId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalReviews = g.Count(),
                AverageRating = g.Average(r => (double)r.Rating)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var totalReviews = summary?.TotalReviews ?? 0;
        var averageRating = summary is null
            ? 0m
            : Math.Round((decimal)summary.AverageRating, 2, MidpointRounding.AwayFromZero);

        if (totalReviews == 0)
        {
            var emptyReviews = new PaginatedList<ReviewDto>([], 0, request.Page, request.Size);
            return new ProductReviewsSummaryDto(request.ProductId, 0m, 0, emptyReviews);
        }

        var items = await reviewRepository
            .AsQueryable(asNoTracking: true)
            .Where(r => r.ProductId == request.ProductId)
            .OrderByDescending(r => r.CreatedAt)
            .ProjectToType<ReviewDto>(mapper.Config)
            .Skip((request.Page - 1) * request.Size)
            .Take(request.Size)
            .ToListAsync(cancellationToken);

        var reviews = new PaginatedList<ReviewDto>(items, totalReviews, request.Page, request.Size);

        return new ProductReviewsSummaryDto(request.ProductId, averageRating, totalReviews, reviews);
    }
}
