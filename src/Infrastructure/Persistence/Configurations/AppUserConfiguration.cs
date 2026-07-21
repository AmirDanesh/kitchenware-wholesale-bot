using KitchenwareBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitchenwareBot.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("Users");
        b.HasKey(x => x.Id);

        b.Property(x => x.Username).HasMaxLength(100);
        b.Property(x => x.FirstName).HasMaxLength(200);
        b.Property(x => x.Phone).HasMaxLength(30);
        b.Property(x => x.DefaultAddress).HasMaxLength(1000);
        b.Property(x => x.Role).HasConversion<int>();

        b.HasIndex(x => x.TelegramId).IsUnique();

        b.Ignore(x => x.IsAdmin);
    }
}
