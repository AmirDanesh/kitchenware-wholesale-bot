using KitchenwareBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitchenwareBot.Infrastructure.Persistence.Configurations;

internal sealed class PaymentSettingsConfiguration : IEntityTypeConfiguration<PaymentSettings>
{
    private static readonly Guid DefaultId = new("10000000-0000-0000-0000-000000000001");

    public void Configure(EntityTypeBuilder<PaymentSettings> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.BankAccountName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.BankAccountNumber).IsRequired().HasMaxLength(50);
        builder.Property(s => s.BankName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.BankNote).HasMaxLength(1000);

        builder.Ignore(s => s.IsShopOpen);

        builder.HasData(new
        {
            Id = DefaultId,
            BankTransferEnabled = false,
            CashEnabled = false,
            BankAccountName = string.Empty,
            BankAccountNumber = string.Empty,
            BankName = string.Empty,
            BankNote = (string?)null
        });
    }
}
