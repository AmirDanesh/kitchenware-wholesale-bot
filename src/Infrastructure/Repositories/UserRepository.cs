using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Interfaces;
using KitchenwareBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KitchenwareBot.Infrastructure.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public async Task<AppUser?> GetByIdAsync(Guid id) =>
        await _context.AppUsers.FindAsync(id);

    public async Task AddAsync(AppUser entity) => await _context.AppUsers.AddAsync(entity);

    public Task UpdateAsync(AppUser entity)
    {
        _context.AppUsers.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user is not null) _context.AppUsers.Remove(user);
    }

    public async Task<AppUser?> GetByTelegramIdAsync(long telegramId) =>
        await _context.AppUsers.FirstOrDefaultAsync(u => u.TelegramId == telegramId);
}
