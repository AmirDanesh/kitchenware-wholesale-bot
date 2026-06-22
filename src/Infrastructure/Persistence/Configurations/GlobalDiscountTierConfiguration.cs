using KitchenwareBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitchenwareBot.Infrastructure.Persistence.Configurations;

internal sealed class GlobalDiscountTierConfiguration : IEntityTypeConfiguration<GlobalDiscountTier>
{
    public void Configure(EntityTypeBuilder<GlobalDiscountTier> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.DiscountPercent).HasPrecision(5, 2);
        builder.Property(t => t.IsActive).HasDefaultValue(true);
        builder.Property(t => t.DisplayOrder).HasDefaultValue(0);

        builder.HasIndex(t => t.IsActive);
        builder.HasIndex(t => t.MinQuantity);
    }
}
