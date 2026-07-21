using KitchenwareBot.Domain.Common;

namespace KitchenwareBot.Domain.Entities;

/// <summary>Singleton row controlling which payment methods are enabled and the bank
/// details shown to customers. The shop is "open" only when at least one method is on.</summary>
public class PaymentSettings : BaseEntity
{
    /// <summary>Well-known Id of the single settings row (singleton pattern).</summary>
    public static readonly Guid SingletonId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public bool BankTransferEnabled { get; private set; }
    public bool CashEnabled { get; private set; }
    public string? BankAccountName { get; private set; }
    public string? BankAccountNumber { get; private set; }
    public string? BankName { get; private set; }
    public string? BankNote { get; private set; }

    public bool IsShopOpen => BankTransferEnabled || CashEnabled;

    private PaymentSettings() { }

    public static PaymentSettings CreateDefault()
        => new()
        {
            Id = SingletonId,
            BankTransferEnabled = false,
            CashEnabled = false
        };

    public void SetBankTransferEnabled(bool enabled) => BankTransferEnabled = enabled;
    public void SetCashEnabled(bool enabled) => CashEnabled = enabled;

    public void UpdateBankDetails(string? bankName, string? accountNumber, string? accountName, string? note)
    {
        BankName = bankName;
        BankAccountNumber = accountNumber;
        BankAccountName = accountName;
        BankNote = note;
    }
}
