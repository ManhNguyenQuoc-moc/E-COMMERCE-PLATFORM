using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.UnitTests.Domain;

public sealed class RefreshTokenTests
{
    [Fact]
    public void Constructor_Should_CreateActiveToken_When_InputsAreValid()
    {
        var accountId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        var token = new RefreshToken(accountId, "hashed_token_value", expiresAt);

        Assert.Equal(accountId, token.AccountId);
        Assert.Equal("hashed_token_value", token.TokenHash);
        Assert.Equal(expiresAt, token.ExpiresAt);
        Assert.True(token.IsActive);
        Assert.False(token.IsRevoked);
        Assert.False(token.IsExpired);
    }

    [Fact]
    public void Constructor_Should_ThrowDomainException_When_ExpirationIsInThePast()
    {
        Assert.Throws<DomainException>(() =>
            new RefreshToken(Guid.NewGuid(), "hashed_token", DateTimeOffset.UtcNow.AddMinutes(-1)));
    }

    [Fact]
    public void Revoke_Should_MarkTokenAsRevokedAndStoreReplacementHash()
    {
        var token = new RefreshToken(Guid.NewGuid(), "old_hash", DateTimeOffset.UtcNow.AddDays(7));

        token.Revoke("new_hash");

        Assert.True(token.IsRevoked);
        Assert.False(token.IsActive);
        Assert.Equal("new_hash", token.ReplacedByTokenHash);
        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public void Constructor_Should_ClampExpiresAtToAbsoluteExpiresAt_When_ExceedingAbsoluteLimit()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        var absoluteExpiresAt = DateTimeOffset.UtcNow.AddDays(2);

        var token = new RefreshToken(Guid.NewGuid(), "clamped_hash", expiresAt, absoluteExpiresAt);

        Assert.Equal(absoluteExpiresAt, token.ExpiresAt);
        Assert.Equal(absoluteExpiresAt, token.AbsoluteExpiresAt);
        Assert.True(token.IsActive);
    }

    [Fact]
    public void Constructor_Should_ThrowDomainException_When_AbsoluteExpirationIsInThePast()
    {
        Assert.Throws<DomainException>(() =>
            new RefreshToken(
                Guid.NewGuid(),
                "hashed_token",
                DateTimeOffset.UtcNow.AddDays(7),
                DateTimeOffset.UtcNow.AddMinutes(-1)));
    }
}
