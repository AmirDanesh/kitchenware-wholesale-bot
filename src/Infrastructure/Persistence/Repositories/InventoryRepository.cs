using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Exceptions;
using KitchenwareBot.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KitchenwareBot.Infrastructure.Persistence.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _db;
    public InventoryRepository(AppDbContext db) => _db = db;

    public Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.InventoryItems.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<InventoryItem?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken ct = default)
        => _db.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId, ct);

    public async Task<IReadOnlyList<InventoryItem>> GetByProductAsync(Guid productId, CancellationToken ct = default)
        => await _db.InventoryItems.Where(i => i.ProductId == productId).ToListAsync(ct);

    public Task<int> GetAvailableStockAsync(Guid productId, CancellationToken ct = default)
        => _db.InventoryItems
            .Where(i => i.ProductId == productId)
            .Select(i => i.Quantity - i.ReservedQuantity)
            .SumAsync(ct);

    public async Task<IReadOnlyList<InventoryItem>> GetAllLowStockAsync(CancellationToken ct = default)
        => await _db.InventoryItems
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .Where(i => i.Product!.IsActive && (i.Quantity - i.ReservedQuantity) <= i.LowStockThreshold)
            .OrderBy(i => i.Quantity - i.ReservedQuantity)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<InventoryItem>> GetWarehouseStockAsync(Guid warehouseId, CancellationToken ct = default)
        => await _db.InventoryItems
            .Include(i => i.Product)
            .Where(i => i.WarehouseId == warehouseId)
            .OrderBy(i => i.Product!.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<InventoryItem>> ReserveAsync(Guid productId, int qty, CancellationToken ct = default)
    {
        var items = await LoadTrackedForProductWithUpdateLockAsync(productId, ct);
        var totalAvailable = items.Sum(i => i.AvailableQuantity);
        if (totalAvailable < qty)
        {
            var name = items.FirstOrDefault()?.Product?.Name ?? string.Empty;
            throw new InsufficientStockException(productId, name, qty, totalAvailable);
        }

        var remaining = qty;
        var newlyLowStock = new List<InventoryItem>();
        foreach (var item in items.OrderByDescending(i => i.AvailableQuantity))
        {
            if (remaining <= 0) break;
            var take = Math.Min(item.AvailableQuantity, remaining);
            if (take <= 0) continue;
            var wasLowStock = item.IsLowStock;
            item.Reserve(take);
            if (!wasLowStock && item.IsLowStock)
                newlyLowStock.Add(item);
            remaining -= take;
        }

        return newlyLowStock;
    }

    public async Task ReleaseAsync(Guid productId, int qty, CancellationToken ct = default)
    {
        var items = await LoadTrackedForProductWithUpdateLockAsync(productId, ct);
        var remaining = qty;
        foreach (var item in items.OrderByDescending(i => i.ReservedQuantity))
        {
            if (remaining <= 0) break;
            var take = Math.Min(item.ReservedQuantity, remaining);
            if (take <= 0) continue;
            item.Release(take);
            remaining -= take;
        }

        if (remaining > 0)
            throw new InvalidOperationException("Cannot release more stock than is reserved.");
    }

    public async Task ConsumeAsync(Guid productId, int qty, CancellationToken ct = default)
    {
        var items = await LoadTrackedForProductWithUpdateLockAsync(productId, ct);
        var remaining = qty;
        // Consume from the rows that hold the reservations first.
        foreach (var item in items.OrderByDescending(i => i.ReservedQuantity).ThenByDescending(i => i.Quantity))
        {
            if (remaining <= 0) break;
            var take = Math.Min(item.ReservedQuantity, remaining);
            if (take <= 0) continue;
            item.Consume(take);
            remaining -= take;
        }

        if (remaining > 0)
            throw new InvalidOperationException("Cannot consume more stock than is reserved.");
    }

    public async Task AddAsync(InventoryItem item, CancellationToken ct = default)
        => await _db.InventoryItems.AddAsync(item, ct);

    private async Task<List<InventoryItem>> LoadTrackedForProductWithUpdateLockAsync(Guid productId, CancellationToken ct)
    {
        if (string.Equals(_db.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory",
                StringComparison.Ordinal))
        {
            return await _db.InventoryItems
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .Where(i => i.ProductId == productId)
                .ToListAsync(ct);
        }

        if (_db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Inventory mutations require an explicit database transaction.");

        return await _db.InventoryItems
            .FromSqlInterpolated($"SELECT * FROM [InventoryItems] WITH (UPDLOCK, HOLDLOCK) WHERE [ProductId] = {productId}")
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .ToListAsync(ct);
    }
}
