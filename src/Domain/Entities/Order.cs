using KitchenwareBot.Domain.Enums;

namespace KitchenwareBot.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public long CustomerTelegramId { get; private set; }
    public string CustomerName { get; private set; } = default!;
    public string? CustomerPhone { get; private set; }
    public OrderStatus Status { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public DeliveryType DeliveryType { get; private set; }
    public string? ShippingAddress { get; private set; }
    public string? AdminNote { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core populates this via backing field — T-18 config must call .HasField("_items")
    private List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public static Order Create(long customerTelegramId, string customerName, string? customerPhone,
        PaymentMethod paymentMethod, DeliveryType deliveryType, string? shippingAddress)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerTelegramId = customerTelegramId,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            Status = OrderStatus.Pending,
            PaymentMethod = paymentMethod,
            DeliveryType = deliveryType,
            ShippingAddress = shippingAddress,
            TotalAmount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void AddItem(Guid productId, string productName, decimal originalPrice,
        decimal discountPercent, int quantity)
    {
        var item = OrderItem.Create(Id, productId, productName, originalPrice, discountPercent, quantity);
        _items.Add(item);
        RecalculateTotal();
    }

    public void UpdateStatus(OrderStatus status, string? note = null)
    {
        Status = status;
        if (note is not null)
            AdminNote = note;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecalculateTotal()
    {
        TotalAmount = _items.Sum(i => i.SubTotal);
        UpdatedAt = DateTime.UtcNow;
    }
}
