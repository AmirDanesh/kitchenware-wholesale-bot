using KitchenwareBot.Domain.Common;
using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Enums;
using KitchenwareBot.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KitchenwareBot.Infrastructure.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;
    public OrderRepository(AppDbContext db) => _db = db;

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<PagedResult<Order>> GetByCustomerAsync(long customerTelegramId, int page, int pageSize, CancellationToken ct = default)
        => _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.CustomerTelegramId == customerTelegramId)
            .OrderByDescending(o => o.CreatedAt)
            .ToPagedResultAsync(page, pageSize, ct);

    public Task<PagedResult<Order>> GetAllAsync(OrderStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();
        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);
        return query.OrderByDescending(o => o.CreatedAt).ToPagedResultAsync(page, pageSize, ct);
    }

    public Task<int> GetPendingCountAsync(CancellationToken ct = default)
        => _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending, ct);

    public async Task AddAsync(Order order, CancellationToken ct = default)
        => await _db.Orders.AddAsync(order, ct);

    public void Update(Order order) => _db.Orders.Update(order);
}
