using KitchenwareBot.Domain.Common;
using KitchenwareBot.Domain.Exceptions;

namespace KitchenwareBot.Domain.Entities;

/// <summary>Per-warehouse stock for a product. Reservation lifecycle:
/// Reserve (on order placed) → Consume (on confirm) or Release (on cancel).</summary>
public class InventoryItem : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public int Quantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int LowStockThreshold { get; private set; } = 5;

    // Navigation
    public Product? Product { get; private set; }
    public Warehouse? Warehouse { get; private set; }

    /// <summary>Stock that can still be sold (not already reserved).</summary>
    public int AvailableQuantity => Quantity - ReservedQuantity;
    public bool IsLowStock => AvailableQuantity <= LowStockThreshold;

    private InventoryItem() { }

    public static InventoryItem Create(Guid productId, Guid warehouseId, int quantity = 0, int lowStockThreshold = 5)
    {
        if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        return new InventoryItem
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Quantity = quantity,
            ReservedQuantity = 0,
            LowStockThreshold = Math.Max(0, lowStockThreshold)
        };
    }

    /// <summary>Absolute stock adjustment (+/-). Cannot push physical quantity below what is reserved.</summary>
    public void Adjust(int delta)
    {
        var newQty = Quantity + delta;
        if (newQty < 0)
            throw new InvalidOperationException("Stock adjustment would make quantity negative.");
        if (newQty < ReservedQuantity)
            throw new InvalidOperationException("Cannot reduce quantity below the reserved amount.");
        Quantity = newQty;
    }

    /// <summary>Reserve stock for a pending order. Throws if not enough is available.</summary>
    public void Reserve(int qty)
    {
        RequirePositive(qty);
        if (qty > AvailableQuantity)
            throw new InsufficientStockException(ProductId, Product?.Name ?? string.Empty, qty, AvailableQuantity);
        ReservedQuantity += qty;
    }

    /// <summary>Release a previous reservation (order cancelled). Quantity is unchanged.</summary>
    public void Release(int qty)
    {
        RequirePositive(qty);
        if (qty > ReservedQuantity)
            throw new InvalidOperationException("Cannot release more than the reserved quantity.");
        ReservedQuantity -= qty;
    }

    /// <summary>Consume stock for a confirmed order: reduces both physical and reserved quantity.</summary>
    public void Consume(int qty)
    {
        RequirePositive(qty);
        if (qty > Quantity)
            throw new InvalidOperationException("Cannot consume more than the physical quantity.");
        if (qty > ReservedQuantity)
            throw new InvalidOperationException("Cannot consume more than the reserved quantity.");
        Quantity -= qty;
        ReservedQuantity -= qty;
    }

    public void SetLowStockThreshold(int threshold) => LowStockThreshold = Math.Max(0, threshold);

    private static void RequirePositive(int qty)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty), "Quantity must be positive.");
    }
}
