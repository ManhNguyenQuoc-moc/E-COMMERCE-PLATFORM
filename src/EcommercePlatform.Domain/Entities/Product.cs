using EcommercePlatform.Domain.Common;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public ProductStatus Status { get; private set; }

    private Product()
    {
    }

    public Product(string name, string description, string sku, decimal price, int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name cannot be empty.");

        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("Product SKU cannot be empty.");

        if (price < 0)
            throw new DomainException("Product price cannot be negative.");

        if (stockQuantity < 0)
            throw new DomainException("Stock quantity cannot be negative.");

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Sku = sku.Trim().ToUpperInvariant();
        Price = price;
        StockQuantity = stockQuantity;
        Status = stockQuantity > 0 ? ProductStatus.Active : ProductStatus.OutOfStock;
    }

    public void UpdateDetails(string name, string description, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name cannot be empty.");

        if (price < 0)
            throw new DomainException("Product price cannot be negative.");

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Price = price;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AdjustStock(int delta)
    {
        var newStock = StockQuantity + delta;
        if (newStock < 0)
            throw new DomainException($"Insufficient stock for product '{Name}'.");

        StockQuantity = newStock;
        Status = StockQuantity == 0 ? ProductStatus.OutOfStock : ProductStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

