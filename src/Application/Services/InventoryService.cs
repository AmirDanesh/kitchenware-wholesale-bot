using KitchenwareBot.Application.Abstractions;
using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Repositories;

namespace KitchenwareBot.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventory;
    private readonly IProductRepository _products;
    private readonly IWarehouseRepository _warehouses;
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notifier;

    public InventoryService(
        IInventoryRepository inventory,
        IProductRepository products,
        IWarehouseRepository warehouses,
        IUnitOfWork uow,
        INotificationService notifier)
    {
        _inventory = inventory;
        _products = products;
        _warehouses = warehouses;
        _uow = uow;
        _notifier = notifier;
    }

    public async Task<IReadOnlyList<StockReportItemDto>> GetStockReportAsync(CancellationToken ct = default)
    {
        var warehouses = await _warehouses.GetAllAsync(activeOnly: false, ct);
        var agg = new Dictionary<Guid, (string Name, int Qty, int Reserved, int Threshold)>();

        foreach (var w in warehouses)
        {
            var items = await _inventory.GetWarehouseStockAsync(w.Id, ct);
            foreach (var i in items)
            {
                if (i.Product is null) continue;
                agg.TryGetValue(i.ProductId, out var e);
                agg[i.ProductId] = (
                    i.Product.Name,
                    e.Qty + i.Quantity,
                    e.Reserved + i.ReservedQuantity,
                    Math.Max(e.Threshold, i.LowStockThreshold));
            }
        }

        return agg
            .Select(kv =>
            {
                var available = kv.Value.Qty - kv.Value.Reserved;
                return new StockReportItemDto(kv.Key, kv.Value.Name, kv.Value.Qty, kv.Value.Reserved,
                    available, available <= kv.Value.Threshold);
            })
            .OrderBy(x => x.ProductName)
            .ToList();
    }

    public async Task<IReadOnlyList<LowStockItemDto>> GetLowStockAlertsAsync(CancellationToken ct = default)
    {
        var items = await _inventory.GetAllLowStockAsync(ct);
        return items
            .Select(i => new LowStockItemDto(
                i.ProductId,
                i.Product?.Name ?? string.Empty,
                i.Warehouse?.Name ?? string.Empty,
                i.AvailableQuantity,
                i.LowStockThreshold))
            .ToList();
    }

    public async Task AdjustStockAsync(Guid productId, Guid warehouseId, int delta, string? reason, CancellationToken ct = default)
    {
        var item = await _inventory.GetByProductAndWarehouseAsync(productId, warehouseId, ct);
        if (item is null)
        {
            if (delta < 0)
                throw new InvalidOperationException("No stock exists for this product/warehouse to decrease.");
            item = InventoryItem.Create(productId, warehouseId, delta);
            await _inventory.AddAsync(item, ct);
        }
        else
        {
            item.Adjust(delta);
        }

        await _uow.SaveChangesAsync(ct);

        // Notify admins if this dropped to/below the low-stock threshold.
        if (item.IsLowStock)
        {
            var product = await _products.GetByIdAsync(productId, ct);
            var warehouse = await _warehouses.GetByIdAsync(warehouseId, ct);
            await _notifier.NotifyAdminsLowStockAsync(
                new LowStockItemDto(productId, product?.Name ?? string.Empty,
                    warehouse?.Name ?? string.Empty, item.AvailableQuantity, item.LowStockThreshold), ct);
        }
    }

    public Task<IReadOnlyList<Warehouse>> GetWarehousesAsync(CancellationToken ct = default)
        => _warehouses.GetAllAsync(activeOnly: true, ct);
}
