namespace KitchenwareBot.Domain.Entities;

public class PaymentSettings
{
    public Guid Id { get; private set; }
    public bool BankTransferEnabled { get; private set; }
    public bool CashEnabled { get; private set; }
    public string BankAccountName { get; private set; } = default!;
    public string BankAccountNumber { get; private set; } = default!;
    public string BankName { get; private set; } = default!;
    public string? BankNote { get; private set; }

    public bool IsShopOpen => BankTransferEnabled || CashEnabled;

    private PaymentSettings() { }

    public static PaymentSettings CreateDefault()
    {
        return new PaymentSettings
        {
            Id = Guid.NewGuid(),
            BankTransferEnabled = false,
            CashEnabled = false,
            BankAccountName = string.Empty,
            BankAccountNumber = string.Empty,
            BankName = string.Empty
        };
    }

    public void Update(bool bankTransferEnabled, bool cashEnabled,
        string bankAccountName, string bankAccountNumber, string bankName, string? bankNote)
    {
        BankTransferEnabled = bankTransferEnabled;
        CashEnabled = cashEnabled;
        BankAccountName = bankAccountName;
        BankAccountNumber = bankAccountNumber;
        BankName = bankName;
        BankNote = bankNote;
    }
}
