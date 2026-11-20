using EcommercePlatform.Domain.Enums;

namespace EcommercePlatform.Application.Common.Interfaces;

public sealed record GeneratedTokenResult(string Token, DateTimeOffset ExpiresAt);

public interface IJwtTokenGenerator
{
    GeneratedTokenResult GenerateAccessToken(
        Guid userId,
        Guid accountId,
        string username,
        string email,
        UserRole role);

    GeneratedTokenResult GenerateRefreshToken();

    string HashRefreshToken(string rawRefreshToken);
}
