namespace KitchenwareBot.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public decimal OriginalPrice { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    public decimal SubTotal => UnitPrice * Quantity;

    public Order Order { get; private set; } = default!;
    public Product Product { get; private set; } = default!;

    private OrderItem() { }

    internal static OrderItem Create(Guid orderId, Guid productId, string productName,
        decimal originalPrice, decimal discountPercent, int quantity)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("تعداد آیتم سفارش باید بیشتر از صفر باشد.");

        var unitPrice = originalPrice * (1 - discountPercent / 100);

        return new OrderItem
        {
            Id = Guid.NewGuid(),
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
