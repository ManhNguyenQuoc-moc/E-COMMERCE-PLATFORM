using EcommercePlatform.Domain.Common;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.Domain.Entities;

public class Cart : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid? AppliedCouponId { get; private set; }

    public User? User { get; private set; }

    private readonly List<CartItem> _items = [];
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    private Cart()
    {
    }

    public Cart(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId is required.");

        UserId = userId;
    }

    public void AddOrIncrementItem(Guid productId, int quantity, int availableStock)
    {
        if (productId == Guid.Empty)
            throw new DomainException("ProductId is required.");

        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        var targetQuantity = existingItem is not null
            ? (long)existingItem.Quantity + quantity
            : quantity;

        if (targetQuantity > availableStock)
            throw new DomainException($"Insufficient stock. Available: {availableStock}, requested total: {targetQuantity}.");

        var resolvedQuantity = (int)targetQuantity;

        if (existingItem is not null)
        {
            existingItem.SetQuantity(resolvedQuantity);
        }
        else
        {
            _items.Add(new CartItem(Id, productId, resolvedQuantity));
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateItemQuantity(Guid cartItemId, int newQuantity, int availableStock)
    {
        var existingItem = _items.FirstOrDefault(i => i.Id == cartItemId)
            ?? throw new NotFoundException(nameof(CartItem), cartItemId);

        if (newQuantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        if (newQuantity > availableStock)
            throw new DomainException($"Insufficient stock. Available: {availableStock}, requested: {newQuantity}.");

        existingItem.SetQuantity(newQuantity);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public CartItem RemoveItem(Guid cartItemId)
    {
        var existingItem = _items.FirstOrDefault(i => i.Id == cartItemId)
            ?? throw new NotFoundException(nameof(CartItem), cartItemId);

        _items.Remove(existingItem);
        UpdatedAt = DateTimeOffset.UtcNow;
        return existingItem;
    }
}
