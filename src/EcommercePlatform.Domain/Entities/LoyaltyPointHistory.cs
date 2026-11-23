using EcommercePlatform.Domain.Common;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.Domain.Entities;

public class LoyaltyPointHistory : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid OrderId { get; private set; }
    public int Points { get; private set; }
    public string Description { get; private set; } = string.Empty;

    public User? User { get; private set; }
    public Order? Order { get; private set; }

    private LoyaltyPointHistory()
    {
    }

    public LoyaltyPointHistory(Guid userId, Guid orderId, int points, string description)
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId is required.");

        if (orderId == Guid.Empty)
            throw new DomainException("OrderId is required.");

        if (points <= 0)
            throw new DomainException("Points must be greater than zero.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description cannot be empty.");

        UserId = userId;
        OrderId = orderId;
        Points = points;
        Description = description.Trim();
    }
}
