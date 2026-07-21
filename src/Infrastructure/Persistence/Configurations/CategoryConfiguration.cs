using KitchenwareBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitchenwareBot.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("Categories");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).IsRequired().HasMaxLength(100);

        b.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.ParentId);
        b.HasIndex(x => x.DisplayOrder);

        // Seed top-level categories (Persian) so admins can create products immediately.
        b.HasData(
            new { Id = SeedData.CategoryIds.Cookware,   Name = "قابلمه و تابه",          ParentId = (Guid?)null, DisplayOrder = 1, IsActive = true },
            new { Id = SeedData.CategoryIds.Tableware,  Name = "سرویس غذاخوری",          ParentId = (Guid?)null, DisplayOrder = 2, IsActive = true },
            new { Id = SeedData.CategoryIds.Appliances, Name = "لوازم برقی آشپزخانه",     ParentId = (Guid?)null, DisplayOrder = 3, IsActive = true },
            new { Id = SeedData.CategoryIds.Storage,    Name = "ظروف نگهداری",           ParentId = (Guid?)null, DisplayOrder = 4, IsActive = true },
            new { Id = SeedData.CategoryIds.Utensils,   Name = "ابزار و لوازم جانبی",     ParentId = (Guid?)null, DisplayOrder = 5, IsActive = true }
        );
    }
}
