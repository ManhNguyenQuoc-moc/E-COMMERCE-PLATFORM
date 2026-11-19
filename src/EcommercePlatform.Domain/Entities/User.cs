using EcommercePlatform.Domain.Common;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.Domain.Entities;

public class User : BaseEntity
{
    public Guid AccountId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? DefaultShippingAddress { get; private set; }
    public int LoyaltyPoints { get; private set; }
    public UserRank Rank { get; private set; } = UserRank.Member;

    public Account? Account { get; private set; }

    private User()
    {
    }

    public User(
        Guid accountId,
        string fullName,
        string? phoneNumber = null,
        string? defaultShippingAddress = null)
    {
        if (accountId == Guid.Empty)
            throw new DomainException("AccountId is required.");

        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Full name cannot be empty.");

        AccountId = accountId;
        FullName = fullName.Trim();
        PhoneNumber = phoneNumber?.Trim();
        DefaultShippingAddress = defaultShippingAddress?.Trim();
        LoyaltyPoints = 0;
        Rank = UserRank.Member;
    }

    public void UpdateProfile(
        string fullName,
        string? phoneNumber,
        string? defaultShippingAddress,
        string? avatarUrl = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Full name cannot be empty.");

        FullName = fullName.Trim();
        PhoneNumber = phoneNumber?.Trim();
        DefaultShippingAddress = defaultShippingAddress?.Trim();
        AvatarUrl = avatarUrl?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddLoyaltyPoints(int points)
    {
        if (points <= 0)
            throw new DomainException("Points to add must be greater than zero.");

        LoyaltyPoints += points;
        Rank = LoyaltyPoints switch
        {
            >= 1000 => UserRank.Diamond,
            >= 500 => UserRank.Gold,
            >= 100 => UserRank.Silver,
            _ => UserRank.Member
        };
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
