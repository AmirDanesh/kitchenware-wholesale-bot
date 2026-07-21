using KitchenwareBot.Domain.Entities;

namespace KitchenwareBot.Domain.Repositories;

public interface IWarehouseRepository
{
    Task<IReadOnlyList<Warehouse>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Warehouse?> GetDefaultAsync(CancellationToken ct = default);
    Task AddAsync(Warehouse warehouse, CancellationToken ct = default);
    void Update(Warehouse warehouse);
}
