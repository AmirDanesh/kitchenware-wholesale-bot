using KitchenwareBot.Domain.Common;
using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Enums;

namespace KitchenwareBot.Domain.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default); // includes Items
    Task<PagedResult<Order>> GetByCustomerAsync(long customerTelegramId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<Order>> GetAllAsync(OrderStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetPendingCountAsync(CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    void Update(Order order);
}
