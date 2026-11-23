using EcommercePlatform.Domain.Common;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.Domain.Entities;

public class Review : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid OrderId { get; private set; }
    public int Rating { get; private set; }
    public string? Comment { get; private set; }

    public Product? Product { get; private set; }
    public User? User { get; private set; }
    public Order? Order { get; private set; }

    private Review()
    {
    }

    public Review(Guid productId, Guid userId, Guid orderId, int rating, string? comment = null)
    {
        if (productId == Guid.Empty)
            throw new DomainException("ProductId is required.");

        if (userId == Guid.Empty)
            throw new DomainException("UserId is required.");

        if (orderId == Guid.Empty)
            throw new DomainException("OrderId is required.");

        if (rating is < 1 or > 5)
            throw new DomainException("Rating must be between 1 and 5.");

        var normalizedComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (normalizedComment is not null && normalizedComment.Length > 2000)
            throw new DomainException("Comment must not exceed 2000 characters.");

        ProductId = productId;
        UserId = userId;
        OrderId = orderId;
        Rating = rating;
        Comment = normalizedComment;
    }
}
