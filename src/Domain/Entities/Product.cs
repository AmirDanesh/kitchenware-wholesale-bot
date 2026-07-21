using KitchenwareBot.Domain.Common;

namespace KitchenwareBot.Domain.Entities;

/// <summary>
/// A sellable product. <see cref="Price"/> is the base unit price in Toman;
/// wholesale discounts are resolved separately. Stock lives in <see cref="InventoryItem"/>.
/// </summary>
public class Product : BaseEntity, IAuditable
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public Guid CategoryId { get; private set; }
    public string? ImagePath { get; private set; }
    public string? TelegramFileId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation
    public Category? Category { get; private set; }
    public ICollection<InventoryItem> InventoryItems { get; private set; } = new List<InventoryItem>();
    public ICollection<OrderItem> OrderItems { get; private set; } = new List<OrderItem>();
    public ICollection<ProductDiscountTier> DiscountTiers { get; private set; } = new List<ProductDiscountTier>();

    private Product() { }

    public static Product Create(string name, string? description, decimal price, Guid categoryId)
    {
        Guard(price);
        return new Product
        {
            Name = (name ?? throw new ArgumentNullException(nameof(name))).Trim(),
            Description = description?.Trim() ?? string.Empty,
            Price = price,
            CategoryId = categoryId,
            IsActive = true
        };
    }

    public void Update(string name, string? description, decimal price, Guid categoryId)
    {
        Guard(price);
        Name = (name ?? throw new ArgumentNullException(nameof(name))).Trim();
        Description = description?.Trim() ?? string.Empty;
        Price = price;
        CategoryId = categoryId;
    }

    public void SetPrice(decimal price)
    {
        Guard(price);
        Price = price;
    }

    public void SetImage(string? imagePath, string? telegramFileId)
    {
        ImagePath = imagePath;
        TelegramFileId = telegramFileId;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void OnCreated(DateTime utcNow) { CreatedAt = utcNow; UpdatedAt = utcNow; }
    public void OnUpdated(DateTime utcNow) => UpdatedAt = utcNow;

    private static void Guard(decimal price)
    {
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
    }
}
