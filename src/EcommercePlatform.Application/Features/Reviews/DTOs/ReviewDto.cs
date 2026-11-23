namespace EcommercePlatform.Application.Features.Reviews.DTOs;

public sealed record ReviewDto(
    Guid Id,
    Guid ProductId,
    Guid UserId,
    string UserFullName,
    Guid OrderId,
    int Rating,
    string? Comment,
    DateTimeOffset CreatedAt);
