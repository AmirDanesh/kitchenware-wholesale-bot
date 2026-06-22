using KitchenwareBot.Domain.Entities;

namespace KitchenwareBot.Domain.Interfaces;

public interface IUserRepository : IRepository<AppUser>
{
    Task<AppUser?> GetByTelegramIdAsync(long telegramId);
}
