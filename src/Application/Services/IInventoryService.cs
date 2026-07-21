using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Domain.Entities;

namespace KitchenwareBot.Application.Services;

public interface IInventoryService
{
    Task<IReadOnlyList<StockReportItemDto>> GetStockReportAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LowStockItemDto>> GetLowStockAlertsAsync(CancellationToken ct = default);
    Task AdjustStockAsync(Guid productId, Guid warehouseId, int delta, string? reason, CancellationToken ct = default);
    Task<IReadOnlyList<Warehouse>> GetWarehousesAsync(CancellationToken ct = default);
}
