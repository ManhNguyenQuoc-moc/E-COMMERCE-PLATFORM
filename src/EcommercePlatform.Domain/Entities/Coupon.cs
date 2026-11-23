using EcommercePlatform.Domain.Common;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.Domain.Entities;

public class Coupon : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public DiscountType DiscountType { get; private set; }
    public decimal Value { get; private set; }
    public decimal MinOrderValue { get; private set; }
    public DateTimeOffset StartDate { get; private set; }
    public DateTimeOffset EndDate { get; private set; }
    public int UsageLimit { get; private set; }
    public int UsedCount { get; private set; }
    public bool IsActive { get; private set; } = true;
    public byte[] RowVersion { get; private set; } = [];

    private Coupon()
    {
    }

    public Coupon(
        string code,
        DiscountType discountType,
        decimal value,
        decimal minOrderValue,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int usageLimit)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Coupon code cannot be empty.");

        if (value <= 0)
            throw new DomainException("Coupon value must be greater than zero.");

        if (discountType == DiscountType.Percentage && value > 100)
            throw new DomainException("Percentage discount value cannot exceed 100.");

        if (minOrderValue < 0)
            throw new DomainException("Minimum order value cannot be negative.");

        if (endDate <= startDate)
            throw new DomainException("End date must be after start date.");

        if (usageLimit <= 0)
            throw new DomainException("Usage limit must be greater than zero.");

        Code = code.Trim().ToUpperInvariant();
        DiscountType = discountType;
        Value = value;
        MinOrderValue = minOrderValue;
        StartDate = startDate;
        EndDate = endDate;
        UsageLimit = usageLimit;
        UsedCount = 0;
        IsActive = true;
    }

    public void ValidateCanApply(decimal orderSubTotal, DateTimeOffset now)
    {
        if (!IsActive)
            throw new DomainException($"Coupon '{Code}' is inactive.");

        if (now < StartDate || now > EndDate)
            throw new DomainException($"Coupon '{Code}' is not valid at this time.");

        if (UsedCount >= UsageLimit)
            throw new DomainException($"Coupon '{Code}' has reached its usage limit.");

        if (orderSubTotal < MinOrderValue)
            throw new DomainException($"Order subtotal must be at least {MinOrderValue:N0} to apply coupon '{Code}'.");
    }

    public decimal CalculateDiscount(decimal orderSubTotal)
    {
        if (orderSubTotal <= 0)
            return 0m;

        var rawDiscount = DiscountType == DiscountType.Percentage
            ? Math.Round(orderSubTotal * (Value / 100m), 2, MidpointRounding.AwayFromZero)
            : Value;

        return Math.Min(rawDiscount, orderSubTotal);
    }

    public void IncrementUsage()
    {
        if (UsedCount >= UsageLimit)
            throw new DomainException($"Coupon '{Code}' has reached its usage limit.");

        UsedCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
