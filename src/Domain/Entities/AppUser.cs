using KitchenwareBot.Domain.Enums;

namespace KitchenwareBot.Domain.Entities;

public class AppUser
{
    public Guid Id { get; private set; }
    public long TelegramId { get; private set; }
    public string? Username { get; private set; }
    public string? FirstName { get; private set; }
    public string? Phone { get; private set; }
    public string? DefaultAddress { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsBanned { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AppUser() { }

    public static AppUser Create(long telegramId, string? username, string? firstName)
    {
        return new AppUser
        {
            Id = Guid.NewGuid(),
            TelegramId = telegramId,
            Username = username,
            FirstName = firstName,
            Role = UserRole.Customer,
            IsBanned = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string? username, string? firstName, string? phone)
    {
        Username = username;
        FirstName = firstName;
        Phone = phone;
    }

    public void SetDefaultAddress(string? address) => DefaultAddress = address;

    public void Ban() => IsBanned = true;
    public void Unban() => IsBanned = false;
    public void SetRole(UserRole role) => Role = role;
}
