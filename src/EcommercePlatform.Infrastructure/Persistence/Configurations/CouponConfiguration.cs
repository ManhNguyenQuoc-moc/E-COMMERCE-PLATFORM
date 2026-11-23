using EcommercePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommercePlatform.Infrastructure.Persistence.Configurations;

public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("Coupons");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(c => c.Code)
            .IsUnique();

        builder.Property(c => c.DiscountType)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(c => c.Value)
            .HasPrecision(18, 2);

        builder.Property(c => c.MinOrderValue)
            .HasPrecision(18, 2);

        builder.Property(c => c.RowVersion)
            .IsRowVersion();
    }
}
