using KitchenwareBot.Domain.Common;
using KitchenwareBot.Domain.Enums;

namespace KitchenwareBot.Domain.Entities;

/// <summary>A Telegram user of the bot. Auto-created on first /start.</summary>
public class AppUser : BaseEntity
{
    public long TelegramId { get; private set; }
    public string? Username { get; private set; }
    public string? FirstName { get; private set; }
    public string? Phone { get; private set; }
    public string? DefaultAddress { get; private set; }
    public UserRole Role { get; private set; } = UserRole.Customer;
    public bool IsBanned { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AppUser() { }

    public static AppUser Create(long telegramId, string? username, string? firstName)
        => new()
        {
            TelegramId = telegramId,
            Username = username,
            FirstName = firstName,
            Role = UserRole.Customer,
            IsBanned = false,
            CreatedAt = DateTime.UtcNow
        };

    public void UpdateProfile(string? username, string? firstName)
    {
        Username = username;
        FirstName = firstName;
    }

    public void SetPhone(string? phone) => Phone = phone;
    public void SetDefaultAddress(string? address) => DefaultAddress = address;

    public void PromoteToAdmin() => Role = UserRole.Admin;
    public void DemoteToCustomer() => Role = UserRole.Customer;

    public void Ban() => IsBanned = true;
    public void Unban() => IsBanned = false;

    public bool IsAdmin => Role == UserRole.Admin;
}
