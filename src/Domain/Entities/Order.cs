using KitchenwareBot.Domain.Common;
using KitchenwareBot.Domain.Enums;
using KitchenwareBot.Domain.Exceptions;

namespace KitchenwareBot.Domain.Entities;

/// <summary>
/// A customer order aggregate. Owns its <see cref="OrderItem"/> lines and enforces
/// the status workflow: Pending → Confirmed → Processing → Shipped → Delivered,
/// with Cancelled reachable from any non-terminal stage.
/// </summary>
public class Order : BaseEntity, IAuditable
{
    private readonly List<OrderItem> _items = new();

    public long CustomerTelegramId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string? CustomerPhone { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public PaymentMethod PaymentMethod { get; private set; }
    public DeliveryType DeliveryType { get; private set; }
    public string? ShippingAddress { get; private set; }
    public string? AdminNote { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    /// <summary>Short, human-friendly order code derived from the Id, e.g. "#1A2B3C4D".</summary>
    public string ShortCode => "#" + Id.ToString("N")[..8].ToUpperInvariant();

    private Order() { }

    public static Order Create(long customerTelegramId, string customerName, string? customerPhone,
        PaymentMethod paymentMethod, DeliveryType deliveryType, string? shippingAddress)
    {
        if (deliveryType == DeliveryType.Shipping && string.IsNullOrWhiteSpace(shippingAddress))
            throw new ArgumentException("Shipping orders require an address.", nameof(shippingAddress));

        return new Order
        {
            CustomerTelegramId = customerTelegramId,
            CustomerName = (customerName ?? string.Empty).Trim(),
            CustomerPhone = customerPhone,
            PaymentMethod = paymentMethod,
            DeliveryType = deliveryType,
            ShippingAddress = deliveryType == DeliveryType.Shipping ? shippingAddress : null,
            Status = OrderStatus.Pending
        };
    }

    public OrderItem AddItem(Guid productId, string productName, decimal originalPrice, decimal discountPercent, int quantity)
    {
        var item = OrderItem.Create(Id, productId, productName, originalPrice, discountPercent, quantity);
        _items.Add(item);
        RecalculateTotal();
        return item;
    }

    public void RecalculateTotal() => TotalAmount = _items.Sum(i => i.SubTotal);

    public void UpdateStatus(OrderStatus target, string? note = null)
    {
        if (!IsValidTransition(Status, target))
            throw new InvalidOrderStatusTransitionException(Status, target);

        Status = target;
        if (!string.IsNullOrWhiteSpace(note))
            AdminNote = note.Trim();
    }

    public void SetAdminNote(string? note) => AdminNote = note?.Trim();

    public bool IsTerminal => Status is OrderStatus.Delivered or OrderStatus.Cancelled;

    public static bool IsValidTransition(OrderStatus from, OrderStatus to)
    {
        if (from == to) return false;
        if (to == OrderStatus.Cancelled)
            return from is not (OrderStatus.Delivered or OrderStatus.Cancelled);

        return (from, to) switch
        {
            (OrderStatus.Pending, OrderStatus.Confirmed) => true,
            (OrderStatus.Confirmed, OrderStatus.Processing) => true,
            (OrderStatus.Processing, OrderStatus.Shipped) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            _ => false
        };
    }

    public void OnCreated(DateTime utcNow) { CreatedAt = utcNow; UpdatedAt = utcNow; }
    public void OnUpdated(DateTime utcNow) => UpdatedAt = utcNow;
}
