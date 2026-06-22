using KitchenwareBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitchenwareBot.Infrastructure.Persistence.Configurations;

internal sealed class ProductDiscountTierConfiguration : IEntityTypeConfiguration<ProductDiscountTier>
{
    public void Configure(EntityTypeBuilder<ProductDiscountTier> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.DiscountPercent).HasPrecision(5, 2);
        builder.Property(t => t.IsActive).HasDefaultValue(true);
        builder.Property(t => t.DisplayOrder).HasDefaultValue(0);

        builder.HasOne(t => t.Product)
            .WithMany(p => p.DiscountTiers)
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.ProductId);
        builder.HasIndex(t => new { t.ProductId, t.IsActive });
    }
}
