using EcommercePlatform.Application.Features.Reviews.DTOs;
using MediatR;

namespace EcommercePlatform.Application.Features.Reviews.Queries.GetProductReviews;

public sealed record GetProductReviewsQuery(
    Guid ProductId,
    int Page = 1,
    int Size = 10) : IRequest<ProductReviewsSummaryDto>;
