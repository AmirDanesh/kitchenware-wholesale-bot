using KitchenwareBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitchenwareBot.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("Orders");
        b.HasKey(x => x.Id);

        b.Property(x => x.CustomerName).IsRequired().HasMaxLength(200);
        b.Property(x => x.CustomerPhone).HasMaxLength(30);
        b.Property(x => x.ShippingAddress).HasMaxLength(1000);
        b.Property(x => x.AdminNote).HasMaxLength(1000);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.PaymentMethod).HasConversion<int>();
        b.Property(x => x.DeliveryType).HasConversion<int>();

        b.HasIndex(x => x.CustomerTelegramId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.CreatedAt);

        b.HasMany(x => x.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Items is exposed as a read-only collection backed by the _items field.
        b.Metadata.FindNavigation(nameof(Order.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        b.Ignore(x => x.IsTerminal);
        b.Ignore(x => x.ShortCode);
    }
}
