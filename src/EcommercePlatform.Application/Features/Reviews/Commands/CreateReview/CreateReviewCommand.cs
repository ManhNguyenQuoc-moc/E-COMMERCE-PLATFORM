using EcommercePlatform.Application.Features.Reviews.DTOs;
using MediatR;

namespace EcommercePlatform.Application.Features.Reviews.Commands.CreateReview;

public sealed record CreateReviewCommand(
    Guid ProductId,
    int Rating,
    string? Comment = null) : IRequest<ReviewDto>;
