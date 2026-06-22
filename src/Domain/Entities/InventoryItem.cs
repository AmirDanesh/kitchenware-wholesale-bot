namespace KitchenwareBot.Domain.Entities;

public class InventoryItem
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public int Quantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int LowStockThreshold { get; private set; } = 5;

    public int AvailableQuantity => Quantity - ReservedQuantity;
    public bool IsLowStock => AvailableQuantity <= LowStockThreshold;

    public Product Product { get; private set; } = default!;
    public Warehouse Warehouse { get; private set; } = default!;

    private InventoryItem() { }

    public static InventoryItem Create(Guid productId, Guid warehouseId, int initialQuantity = 0, int lowStockThreshold = 5)
    {
        if (initialQuantity < 0)
            throw new InvalidOperationException("موجودی اولیه نمی‌تواند منفی باشد.");

        return new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            WarehouseId = warehouseId,
            Quantity = initialQuantity,
            ReservedQuantity = 0,
            LowStockThreshold = lowStockThreshold
        };
    }

    // Admin adjusts total stock (positive or negative delta)
    public void Adjust(int delta)
    {
        var newQuantity = Quantity + delta;
        if (newQuantity < 0)
            throw new InvalidOperationException("موجودی نمی‌تواند منفی باشد.");
        if (newQuantity < ReservedQuantity)
            throw new InvalidOperationException("موجودی نمی‌تواند از مقدار رزرو شده کمتر باشد.");

        Quantity = newQuantity;
    }

    // Called when customer places an order
    public void Reserve(int qty)
    {
        if (qty <= 0)
            throw new InvalidOperationException("مقدار رزرو باید بیشتر از صفر باشد.");
        if (AvailableQuantity < qty)
            throw new InvalidOperationException("موجودی کافی نیست.");

        ReservedQuantity += qty;
    }

    // Called when order is cancelled
    public void Release(int qty)
    {
        if (qty <= 0)
            throw new InvalidOperationException("مقدار آزادسازی باید بیشتر از صفر باشد.");
        if (ReservedQuantity < qty)
            throw new InvalidOperationException("مقدار رزرو شده کمتر از مقدار درخواستی است.");

        ReservedQuantity -= qty;
    }

    // Called when admin confirms an order — deducts from total stock and reservation
    public void Consume(int qty)
    {
        if (qty <= 0)
            throw new InvalidOperationException("مقدار مصرف باید بیشتر از صفر باشد.");
        if (ReservedQuantity < qty)
            throw new InvalidOperationException("مقدار رزرو شده کمتر از مقدار درخواستی است.");
        if (Quantity < qty)
            throw new InvalidOperationException("موجودی کل کافی نیست.");

        Quantity -= qty;
        ReservedQuantity -= qty;
    }

    public void SetLowStockThreshold(int threshold)
    {
        if (threshold < 0)
            throw new InvalidOperationException("حد هشدار موجودی نمی‌تواند منفی باشد.");

        LowStockThreshold = threshold;
    }
}
