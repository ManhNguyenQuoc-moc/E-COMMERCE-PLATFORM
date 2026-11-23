using EcommercePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommercePlatform.Infrastructure.Persistence.Configurations;

public sealed class LoyaltyPointHistoryConfiguration : IEntityTypeConfiguration<LoyaltyPointHistory>
{
    public void Configure(EntityTypeBuilder<LoyaltyPointHistory> builder)
    {
        builder.ToTable("LoyaltyPointHistories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Description)
            .IsRequired()
            .HasMaxLength(300);

        builder.HasIndex(h => new { h.UserId, h.CreatedAt })
            .IsDescending(false, true);

        builder.HasIndex(h => h.OrderId)
            .IsUnique();

        builder.HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Order)
            .WithMany()
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
