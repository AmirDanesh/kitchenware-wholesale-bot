using KitchenwareBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitchenwareBot.Infrastructure.Persistence.Configurations;

public class GlobalDiscountTierConfiguration : IEntityTypeConfiguration<GlobalDiscountTier>
{
    public void Configure(EntityTypeBuilder<GlobalDiscountTier> b)
    {
        b.ToTable("GlobalDiscountTiers");
        b.HasKey(x => x.Id);

        // DiscountPercent precision comes from the global 18,2 decimal convention.
        b.HasIndex(x => x.MinQuantity);
        b.HasIndex(x => x.DisplayOrder);
    }
}
