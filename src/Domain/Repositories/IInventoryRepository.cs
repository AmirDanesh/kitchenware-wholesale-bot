using KitchenwareBot.Domain.Entities;

namespace KitchenwareBot.Domain.Repositories;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<InventoryItem?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryItem>> GetByProductAsync(Guid productId, CancellationToken ct = default);
    Task<int> GetAvailableStockAsync(Guid productId, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryItem>> GetAllLowStockAsync(CancellationToken ct = default);
    Task<IReadOnlyList<InventoryItem>> GetWarehouseStockAsync(Guid warehouseId, CancellationToken ct = default);

    // Reservation lifecycle. These mutate tracked entities; the caller commits via IUnitOfWork
    // (so a whole order can be reserved atomically). ReserveAsync throws InsufficientStockException.
    Task ReserveAsync(Guid productId, int qty, CancellationToken ct = default);
    Task ReleaseAsync(Guid productId, int qty, CancellationToken ct = default);
    Task ConsumeAsync(Guid productId, int qty, CancellationToken ct = default);

    Task AddAsync(InventoryItem item, CancellationToken ct = default);
}
