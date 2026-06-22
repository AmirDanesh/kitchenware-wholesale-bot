using KitchenwareBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitchenwareBot.Infrastructure.Persistence.Configurations;

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    internal static readonly Guid DefaultWarehouseId = new("20000000-0000-0000-0000-000000000001");

    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Name).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Location).HasMaxLength(500);
        builder.Property(w => w.IsActive).HasDefaultValue(true);

        builder.HasData(new
        {
            Id = DefaultWarehouseId,
            Name = "انبار اصلی",
            Location = (string?)null,
            IsActive = true
        });
    }
}
