using EcommercePlatform.Domain.Common;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid AccountId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset AbsoluteExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public Account? Account { get; private set; }

    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt || DateTimeOffset.UtcNow >= AbsoluteExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken()
    {
    }

    public RefreshToken(
        Guid accountId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset? absoluteExpiresAt = null)
    {
        if (accountId == Guid.Empty)
            throw new DomainException("AccountId is required.");

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Token hash cannot be empty.");

        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new DomainException("Expiration time must be in the future.");

        var resolvedAbsolute = absoluteExpiresAt
            ?? (expiresAt > DateTimeOffset.UtcNow.AddDays(30) ? expiresAt : DateTimeOffset.UtcNow.AddDays(30));

        if (resolvedAbsolute <= DateTimeOffset.UtcNow)
            throw new DomainException("Absolute expiration time must be in the future.");

        AccountId = accountId;
        TokenHash = tokenHash.Trim();
        ExpiresAt = expiresAt <= resolvedAbsolute ? expiresAt : resolvedAbsolute;
        AbsoluteExpiresAt = resolvedAbsolute;
    }

    public void Revoke(string? replacedByTokenHash = null)
    {
        if (IsRevoked)
            return;

        RevokedAt = DateTimeOffset.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
