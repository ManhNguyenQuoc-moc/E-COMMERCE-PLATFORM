using EcommercePlatform.Domain.Common;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.Domain.Entities;

public class Order : BaseEntity
{
    private readonly List<OrderItem> _items = [];

    public Guid UserId { get; private set; }
    public Guid? CouponId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public decimal SubTotal { get; private set; }
    public decimal CouponDiscountAmount { get; private set; }
    public decimal RankDiscountAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string ShippingRecipientName { get; private set; } = string.Empty;
    public string ShippingPhone { get; private set; } = string.Empty;
    public string ShippingAddress { get; private set; } = string.Empty;
    public string? Notes { get; private set; }

    public User? User { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order()
    {
    }

    public Order(
        Guid userId,
        string orderNumber,
        string shippingRecipientName,
        string shippingPhone,
        string shippingAddress,
        string? notes = null,
        Guid? couponId = null)
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId is required.");

        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new DomainException("Order number cannot be empty.");

        if (string.IsNullOrWhiteSpace(shippingRecipientName))
            throw new DomainException("Shipping recipient name cannot be empty.");

        if (string.IsNullOrWhiteSpace(shippingPhone))
            throw new DomainException("Shipping phone cannot be empty.");

        if (string.IsNullOrWhiteSpace(shippingAddress))
            throw new DomainException("Shipping address cannot be empty.");

        UserId = userId;
        OrderNumber = orderNumber.Trim();
        ShippingRecipientName = shippingRecipientName.Trim();
        ShippingPhone = shippingPhone.Trim();
        ShippingAddress = shippingAddress.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CouponId = couponId;
    }

    public void AddItem(Guid productId, string productName, string productSku, decimal unitPrice, int quantity)
    {
        if (productId == Guid.Empty)
            throw new DomainException("ProductId is required.");

        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("Product name cannot be empty.");

        if (string.IsNullOrWhiteSpace(productSku))
            throw new DomainException("Product SKU cannot be empty.");

        if (quantity <= 0)
            throw new DomainException("Item quantity must be greater than zero.");

        if (unitPrice < 0)
            throw new DomainException("Unit price cannot be negative.");

        _items.Add(new OrderItem(Id, productId, productName.Trim(), productSku.Trim(), unitPrice, quantity));
        RecalculateTotals();
    }

    public void ApplyDiscounts(UserRank userRank, decimal couponDiscountAmount = 0m)
    {
        if (couponDiscountAmount < 0)
            throw new DomainException("Coupon discount amount cannot be negative.");

        var rankRate = userRank switch
        {
            UserRank.Diamond => 0.10m,
            UserRank.Gold => 0.05m,
            UserRank.Silver => 0.02m,
            _ => 0m
        };

        RankDiscountAmount = Math.Round(SubTotal * rankRate, 2, MidpointRounding.AwayFromZero);
        var remainingAfterRank = Math.Max(0m, SubTotal - RankDiscountAmount);
        CouponDiscountAmount = Math.Min(couponDiscountAmount, remainingAfterRank);
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        SubTotal = _items.Sum(i => i.UnitPrice * i.Quantity);
        TotalAmount = Math.Max(0m, SubTotal - CouponDiscountAmount - RankDiscountAmount);
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
