using KitchenwareBot.Domain.Entities;

namespace KitchenwareBot.Application.Services;

public interface IUserService
{
    Task<AppUser> GetOrCreateAsync(long telegramId, string? username, string? firstName, CancellationToken ct = default);
    Task<bool> IsAdminAsync(long telegramId, CancellationToken ct = default);
    Task<bool> IsBannedAsync(long telegramId, CancellationToken ct = default);
    Task BanAsync(long telegramId, CancellationToken ct = default);
    Task UnbanAsync(long telegramId, CancellationToken ct = default);
    Task SetPhoneAsync(long telegramId, string phone, CancellationToken ct = default);
    Task SetDefaultAddressAsync(long telegramId, string address, CancellationToken ct = default);
}
