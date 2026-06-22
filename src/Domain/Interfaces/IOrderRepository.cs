using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Enums;

namespace KitchenwareBot.Domain.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<IReadOnlyList<Order>> GetByCustomerAsync(long telegramId, int page, int pageSize);
    Task<int> GetByCustomerCountAsync(long telegramId);
    Task<IReadOnlyList<Order>> GetAllAsync(OrderStatus? status, int page, int pageSize);
    Task<int> GetCountAsync(OrderStatus? status);
    Task<int> GetPendingCountAsync();
}
