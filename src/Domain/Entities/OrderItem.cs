using KitchenwareBot.Domain.Common;

namespace KitchenwareBot.Domain.Entities;

/// <summary>
/// A line on an order. All pricing fields are snapshots taken at order time and are
/// never recalculated afterwards (prices are locked once the order is placed).
/// </summary>
public class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty; // snapshot
    public decimal UnitPrice { get; private set; }                  // snapshot: price paid per unit (after discount)
    public decimal OriginalPrice { get; private set; }              // snapshot: base unit price before discount
    public decimal DiscountPercent { get; private set; }            // snapshot: resolved discount %
    public int Quantity { get; private set; }

    // Navigation
    public Order? Order { get; private set; }
    public Product? Product { get; private set; }

    public decimal SubTotal => UnitPrice * Quantity;
    public decimal SavedAmount => (OriginalPrice - UnitPrice) * Quantity;

    private OrderItem() { }

    public static OrderItem Create(Guid orderId, Guid productId, string productName,
        decimal originalPrice, decimal discountPercent, int quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (originalPrice < 0) throw new ArgumentOutOfRangeException(nameof(originalPrice));
        if (discountPercent is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(discountPercent));

        var unitPrice = Math.Round(originalPrice * (1 - discountPercent / 100m), 2, MidpointRounding.AwayFromZero);
        return new OrderItem
        {
            OrderId = orderId,
            ProductId = productId,
            ProductName = productName,
            OriginalPrice = originalPrice,
            DiscountPercent = discountPercent,
            UnitPrice = unitPrice,
            Quantity = quantity
        };
    }
}
