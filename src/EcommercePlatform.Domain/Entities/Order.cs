using EcommercePlatform.Domain.Common;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.Domain.Entities;

public class Order : BaseEntity
{
    private readonly List<OrderItem> _items = [];

    public string OrderNumber { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order()
    {
    }

    public Order(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new DomainException("Order number cannot be empty.");

        OrderNumber = orderNumber.Trim();
    }

    public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Item quantity must be greater than zero.");

        if (unitPrice < 0)
            throw new DomainException("Unit price cannot be negative.");

        _items.Add(new OrderItem(Id, productId, productName, unitPrice, quantity));
        RecalculateTotal();
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Only pending orders can be confirmed.");

        if (_items.Count == 0)
            throw new DomainException("Cannot confirm an empty order.");

        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Sum(i => i.UnitPrice * i.Quantity);
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
