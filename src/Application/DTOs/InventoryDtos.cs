namespace KitchenwareBot.Application.DTOs;

public record StockReportItemDto(
    Guid ProductId,
    string ProductName,
    int TotalQuantity,
    int TotalReserved,
    int TotalAvailable,
    bool IsLowStock);

public record LowStockItemDto(
    Guid ProductId,
    string ProductName,
    string WarehouseName,
    int Available,
    int Threshold);
