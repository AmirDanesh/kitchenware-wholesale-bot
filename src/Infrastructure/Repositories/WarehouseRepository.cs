using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Interfaces;
using KitchenwareBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KitchenwareBot.Infrastructure.Repositories;

internal sealed class WarehouseRepository : IWarehouseRepository
{
    private readonly AppDbContext _context;

    public WarehouseRepository(AppDbContext context) => _context = context;

    public async Task<Warehouse?> GetByIdAsync(Guid id) =>
        await _context.Warehouses.FindAsync(id);

    public async Task AddAsync(Warehouse entity) => await _context.Warehouses.AddAsync(entity);

    public Task UpdateAsync(Warehouse entity)
    {
        _context.Warehouses.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var warehouse = await _context.Warehouses.FindAsync(id);
        if (warehouse is not null) _context.Warehouses.Remove(warehouse);
    }

    public async Task<IReadOnlyList<Warehouse>> GetAllActiveAsync() =>
        await _context.Warehouses
            .Where(w => w.IsActive)
            .OrderBy(w => w.Name)
            .ToListAsync();
}
