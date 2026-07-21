using KitchenwareBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitchenwareBot.Infrastructure.Persistence.Configurations;

public class ProductDiscountTierConfiguration : IEntityTypeConfiguration<ProductDiscountTier>
{
    public void Configure(EntityTypeBuilder<ProductDiscountTier> b)
    {
        b.ToTable("ProductDiscountTiers");
        b.HasKey(x => x.Id);

        b.HasOne(x => x.Product)
            .WithMany(p => p.DiscountTiers)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.ProductId);
        b.HasIndex(x => new { x.ProductId, x.MinQuantity });
    }
}
