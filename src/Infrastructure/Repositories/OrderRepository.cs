using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Enums;
using KitchenwareBot.Domain.Interfaces;
using KitchenwareBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KitchenwareBot.Infrastructure.Repositories;

internal sealed class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context) => _context = context;

    public async Task<Order?> GetByIdAsync(Guid id) =>
        await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);

    public async Task AddAsync(Order entity) => await _context.Orders.AddAsync(entity);

    public Task UpdateAsync(Order entity)
    {
        _context.Orders.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order is not null) _context.Orders.Remove(order);
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(long telegramId, int page, int pageSize)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerTelegramId == telegramId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetByCustomerCountAsync(long telegramId) =>
        await _context.Orders.CountAsync(o => o.CustomerTelegramId == telegramId);

    public async Task<IReadOnlyList<Order>> GetAllAsync(OrderStatus? status, int page, int pageSize)
    {
        var query = _context.Orders.Include(o => o.Items).AsQueryable();
        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(OrderStatus? status)
    {
        var query = _context.Orders.AsQueryable();
        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);
        return await query.CountAsync();
    }

    public async Task<int> GetPendingCountAsync() =>
        await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
}
