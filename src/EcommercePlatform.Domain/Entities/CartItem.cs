using EcommercePlatform.Domain.Common;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.Domain.Entities;

public class CartItem : BaseEntity
{
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public Cart? Cart { get; private set; }
    public Product? Product { get; private set; }

    private CartItem()
    {
    }

    public CartItem(Guid cartId, Guid productId, int quantity)
    {
        if (cartId == Guid.Empty)
            throw new DomainException("CartId is required.");

        if (productId == Guid.Empty)
            throw new DomainException("ProductId is required.");

        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        CartId = cartId;
        ProductId = productId;
        Quantity = quantity;
    }

    internal void SetQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        Quantity = quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
