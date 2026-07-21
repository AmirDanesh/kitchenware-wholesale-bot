using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KitchenwareBot.Infrastructure.Persistence.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly AppDbContext _db;
    public WarehouseRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Warehouse>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var query = _db.Warehouses.AsNoTracking().AsQueryable();
        if (activeOnly) query = query.Where(w => w.IsActive);
        return await query.OrderBy(w => w.Name).ToListAsync(ct);
    }

    public Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Warehouses.FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<Warehouse?> GetDefaultAsync(CancellationToken ct = default)
        => await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == Warehouse.DefaultId, ct)
           ?? await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.Name).FirstOrDefaultAsync(ct);

    public async Task AddAsync(Warehouse warehouse, CancellationToken ct = default)
        => await _db.Warehouses.AddAsync(warehouse, ct);

    public void Update(Warehouse warehouse) => _db.Warehouses.Update(warehouse);
}
