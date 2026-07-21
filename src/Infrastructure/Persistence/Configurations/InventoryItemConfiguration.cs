using KitchenwareBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitchenwareBot.Infrastructure.Persistence.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> b)
    {
        b.ToTable("InventoryItems");
        b.HasKey(x => x.Id);

        b.Property(x => x.LowStockThreshold).HasDefaultValue(5);

        b.HasOne(x => x.Product)
            .WithMany(p => p.InventoryItems)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Warehouse)
            .WithMany(w => w.InventoryItems)
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // One inventory row per (product, warehouse).
        b.HasIndex(x => new { x.ProductId, x.WarehouseId }).IsUnique();

        // Computed, not persisted.
        b.Ignore(x => x.AvailableQuantity);
        b.Ignore(x => x.IsLowStock);
    }
}
