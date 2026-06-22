using KitchenwareBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitchenwareBot.Infrastructure.Persistence.Configurations;

internal sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Quantity).HasDefaultValue(0);
        builder.Property(i => i.ReservedQuantity).HasDefaultValue(0);
        builder.Property(i => i.LowStockThreshold).HasDefaultValue(5);

        builder.Ignore(i => i.AvailableQuantity);
        builder.Ignore(i => i.IsLowStock);

        builder.HasOne(i => i.Product)
            .WithMany(p => p.InventoryItems)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Warehouse)
            .WithMany(w => w.InventoryItems)
            .HasForeignKey(i => i.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optimistic concurrency token — EF throws DbUpdateConcurrencyException when two
        // concurrent transactions both read and update the same InventoryItem row.
        // Callers of ReserveAsync should catch DbUpdateConcurrencyException and retry.
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasIndex(i => i.ProductId);
        builder.HasIndex(i => new { i.ProductId, i.WarehouseId }).IsUnique();
    }
}
