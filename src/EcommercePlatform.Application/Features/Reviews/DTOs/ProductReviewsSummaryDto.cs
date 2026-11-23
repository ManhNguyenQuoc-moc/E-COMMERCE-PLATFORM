using EcommercePlatform.Application.Common.Models;

namespace EcommercePlatform.Application.Features.Reviews.DTOs;

public sealed record ProductReviewsSummaryDto(
    Guid ProductId,
    decimal AverageRating,
    int TotalReviews,
    PaginatedList<ReviewDto> Reviews);
