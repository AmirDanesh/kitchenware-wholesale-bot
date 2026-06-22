using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Interfaces;
using KitchenwareBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KitchenwareBot.Infrastructure.Repositories;

internal sealed class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _context;

    public InventoryRepository(AppDbContext context) => _context = context;

    public async Task<InventoryItem?> GetByIdAsync(Guid id) =>
        await _context.InventoryItems
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .FirstOrDefaultAsync(i => i.Id == id);

    public async Task AddAsync(InventoryItem entity) => await _context.InventoryItems.AddAsync(entity);

    public Task UpdateAsync(InventoryItem entity)
    {
        _context.InventoryItems.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _context.InventoryItems.FindAsync(id);
        if (item is not null) _context.InventoryItems.Remove(item);
    }

    public async Task<int> GetAvailableStockAsync(Guid productId)
    {
        var items = await _context.InventoryItems
            .Where(i => i.ProductId == productId)
            .ToListAsync();
        return items.Sum(i => i.AvailableQuantity);
    }

    public async Task<IReadOnlyList<InventoryItem>> GetAllLowStockAsync()
    {
        return await _context.InventoryItems
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .Where(i => (i.Quantity - i.ReservedQuantity) <= i.LowStockThreshold)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<InventoryItem>> GetWarehouseStockAsync(Guid warehouseId)
    {
        return await _context.InventoryItems
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .Where(i => i.WarehouseId == warehouseId)
            .OrderBy(i => i.Product.Name)
            .ToListAsync();
    }

    // All three mutation methods sort by Id for consistency — Reserve, Release, and Consume
    // must process warehouses in the same order so multi-warehouse distributions are
    // correctly undone. Callers must handle DbUpdateConcurrencyException (thrown by EF Core
    // when the rowversion concurrency token detects a concurrent update) and retry.
    public async Task ReserveAsync(Guid productId, int qty)
    {
        var items = await _context.InventoryItems
            .Where(i => i.ProductId == productId)
            .OrderBy(i => i.Id)
            .ToListAsync();

        if (items.Sum(i => i.AvailableQuantity) < qty)
            throw new InvalidOperationException("موجودی کافی نیست.");

        var remaining = qty;
        foreach (var item in items)
        {
            if (remaining <= 0) break;
            var toReserve = Math.Min(item.AvailableQuantity, remaining);
            if (toReserve > 0)
            {
                item.Reserve(toReserve);
                remaining -= toReserve;
            }
        }
    }

    public async Task ReleaseAsync(Guid productId, int qty)
    {
        var items = await _context.InventoryItems
            .Where(i => i.ProductId == productId)
            .OrderBy(i => i.Id)
            .ToListAsync();

        if (items.Sum(i => i.ReservedQuantity) < qty)
            throw new InvalidOperationException("مقدار رزرو شده کمتر از مقدار درخواستی است.");

        var remaining = qty;
        foreach (var item in items)
        {
            if (remaining <= 0) break;
            var toRelease = Math.Min(item.ReservedQuantity, remaining);
            if (toRelease > 0)
            {
                item.Release(toRelease);
                remaining -= toRelease;
            }
        }
    }

    public async Task ConsumeAsync(Guid productId, int qty)
    {
        var items = await _context.InventoryItems
            .Where(i => i.ProductId == productId)
            .OrderBy(i => i.Id)
            .ToListAsync();

        if (items.Sum(i => i.ReservedQuantity) < qty)
            throw new InvalidOperationException("مقدار رزرو شده کمتر از مقدار درخواستی است.");

        var remaining = qty;
        foreach (var item in items)
        {
            if (remaining <= 0) break;
            var toConsume = Math.Min(item.ReservedQuantity, remaining);
            if (toConsume > 0)
            {
                item.Consume(toConsume);
                remaining -= toConsume;
            }
        }
    }
}
