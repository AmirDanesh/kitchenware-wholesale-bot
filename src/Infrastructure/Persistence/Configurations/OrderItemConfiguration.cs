using KitchenwareBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitchenwareBot.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.ToTable("OrderItems");
        b.HasKey(x => x.Id);

        b.Property(x => x.ProductName).IsRequired().HasMaxLength(200);
        // UnitPrice/OriginalPrice/DiscountPercent precision from the global 18,2 convention.

        b.HasOne(x => x.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.OrderId);

        b.Ignore(x => x.SubTotal);
        b.Ignore(x => x.SavedAmount);
    }
}
