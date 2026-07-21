using KitchenwareBot.Application.Configuration;
using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Enums;
using KitchenwareBot.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace KitchenwareBot.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly long[] _configuredAdminIds;

    public UserService(IUserRepository users, IUnitOfWork uow, IOptions<AdminOptions> adminOptions)
    {
        _users = users;
        _uow = uow;
        _configuredAdminIds = adminOptions.Value.AdminIds ?? Array.Empty<long>();
    }

    public async Task<AppUser> GetOrCreateAsync(long telegramId, string? username, string? firstName, CancellationToken ct = default)
    {
        var user = await _users.GetByTelegramIdAsync(telegramId, ct);
        if (user is null)
        {
            user = AppUser.Create(telegramId, username, firstName);
            if (_configuredAdminIds.Contains(telegramId))
                user.PromoteToAdmin();
            await _users.AddAsync(user, ct);
            await _uow.SaveChangesAsync(ct);
            return user;
        }

        // Keep profile fresh, and ensure configured admins keep their role.
        var changed = false;
        if (user.Username != username || user.FirstName != firstName)
        {
            user.UpdateProfile(username, firstName);
            changed = true;
        }
        if (_configuredAdminIds.Contains(telegramId) && !user.IsAdmin)
        {
            user.PromoteToAdmin();
            changed = true;
        }
        if (changed) await _uow.SaveChangesAsync(ct);
        return user;
    }

    public async Task<bool> IsAdminAsync(long telegramId, CancellationToken ct = default)
    {
        // Config list overrides DB role (failsafe access).
        if (_configuredAdminIds.Contains(telegramId)) return true;
        var user = await _users.GetByTelegramIdAsync(telegramId, ct);
        return user is { Role: UserRole.Admin };
    }

    public async Task<bool> IsBannedAsync(long telegramId, CancellationToken ct = default)
    {
        var user = await _users.GetByTelegramIdAsync(telegramId, ct);
        return user is { IsBanned: true };
    }

    public async Task BanAsync(long telegramId, CancellationToken ct = default)
    {
        var user = await _users.GetByTelegramIdAsync(telegramId, ct);
        if (user is null) return;
        user.Ban();
        await _uow.SaveChangesAsync(ct);
    }

    public async Task UnbanAsync(long telegramId, CancellationToken ct = default)
    {
        var user = await _users.GetByTelegramIdAsync(telegramId, ct);
        if (user is null) return;
        user.Unban();
        await _uow.SaveChangesAsync(ct);
    }

    public async Task SetPhoneAsync(long telegramId, string phone, CancellationToken ct = default)
    {
        var user = await _users.GetByTelegramIdAsync(telegramId, ct);
        if (user is null) return;
        user.SetPhone(phone);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task SetDefaultAddressAsync(long telegramId, string address, CancellationToken ct = default)
    {
        var user = await _users.GetByTelegramIdAsync(telegramId, ct);
        if (user is null) return;
        user.SetDefaultAddress(address);
        await _uow.SaveChangesAsync(ct);
    }
}
