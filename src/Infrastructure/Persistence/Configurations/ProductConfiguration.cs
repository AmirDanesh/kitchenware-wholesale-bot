using KitchenwareBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitchenwareBot.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(300);
        builder.Property(p => p.Description).IsRequired().HasMaxLength(2000);
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.ImagePath).HasMaxLength(500);
        builder.Property(p => p.TelegramFileId).HasMaxLength(200);
        builder.Property(p => p.IsActive).HasDefaultValue(true);

        builder.HasIndex(p => p.IsActive);
        builder.HasIndex(p => p.CategoryId);
    }
}
