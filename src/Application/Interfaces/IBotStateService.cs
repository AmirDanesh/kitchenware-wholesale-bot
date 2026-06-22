using KitchenwareBot.Application.Models;

namespace KitchenwareBot.Application.Interfaces;

public interface IBotStateService
{
    Task<UserSession> GetOrCreateAsync(long telegramId);
    Task SetAsync(long telegramId, UserSession session);
    Task ClearAsync(long telegramId);
}
