using KitchenwareBot.Domain.Entities;

namespace KitchenwareBot.Domain.Interfaces;

public interface IInventoryRepository : IRepository<InventoryItem>
{
    Task<int> GetAvailableStockAsync(Guid productId);
    Task<IReadOnlyList<InventoryItem>> GetAllLowStockAsync();
    Task<IReadOnlyList<InventoryItem>> GetWarehouseStockAsync(Guid warehouseId);
    Task ReserveAsync(Guid productId, int qty);
    Task ReleaseAsync(Guid productId, int qty);
    Task ConsumeAsync(Guid productId, int qty);
}
