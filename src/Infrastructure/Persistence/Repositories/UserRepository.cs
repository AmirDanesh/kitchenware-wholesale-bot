using KitchenwareBot.Domain.Common;
using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KitchenwareBot.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<AppUser?> GetByTelegramIdAsync(long telegramId, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId, ct);

    public Task<PagedResult<AppUser>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
        => _db.Users.AsNoTracking().OrderByDescending(u => u.CreatedAt).ToPagedResultAsync(page, pageSize, ct);

    public async Task AddAsync(AppUser user, CancellationToken ct = default)
        => await _db.Users.AddAsync(user, ct);

    public void Update(AppUser user) => _db.Users.Update(user);
}
