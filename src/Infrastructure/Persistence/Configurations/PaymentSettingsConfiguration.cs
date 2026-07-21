using KitchenwareBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitchenwareBot.Infrastructure.Persistence.Configurations;

public class PaymentSettingsConfiguration : IEntityTypeConfiguration<PaymentSettings>
{
    public void Configure(EntityTypeBuilder<PaymentSettings> b)
    {
        b.ToTable("PaymentSettings");
        b.HasKey(x => x.Id);

        b.Property(x => x.BankAccountName).HasMaxLength(200);
        b.Property(x => x.BankAccountNumber).HasMaxLength(50);
        b.Property(x => x.BankName).HasMaxLength(150);
        b.Property(x => x.BankNote).HasMaxLength(1000);

        b.Ignore(x => x.IsShopOpen);

        // Seed the singleton settings row with both methods disabled (shop starts closed).
        b.HasData(new { Id = PaymentSettings.SingletonId, BankTransferEnabled = false, CashEnabled = false });
    }
}
