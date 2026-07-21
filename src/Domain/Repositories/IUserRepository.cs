using KitchenwareBot.Domain.Common;
using KitchenwareBot.Domain.Entities;

namespace KitchenwareBot.Domain.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AppUser?> GetByTelegramIdAsync(long telegramId, CancellationToken ct = default);
    Task<PagedResult<AppUser>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(AppUser user, CancellationToken ct = default);
    void Update(AppUser user);
}
