namespace EcommercePlatform.Application.Features.Auth.DTOs;

public sealed record AuthResponseDto(
    Guid UserId,
    Guid AccountId,
    string Username,
    string Email,
    string FullName,
    string Role,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
