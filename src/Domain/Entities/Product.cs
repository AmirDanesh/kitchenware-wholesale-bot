namespace KitchenwareBot.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal Price { get; private set; }
    public Guid CategoryId { get; private set; }
    public string? ImagePath { get; private set; }
    public string? TelegramFileId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Category Category { get; private set; } = default!;
    public ICollection<InventoryItem> InventoryItems { get; private set; } = new List<InventoryItem>();
    public ICollection<OrderItem> OrderItems { get; private set; } = new List<OrderItem>();
    public ICollection<ProductDiscountTier> DiscountTiers { get; private set; } = new List<ProductDiscountTier>();

    private Product() { }

    public static Product Create(string name, string description, decimal price, Guid categoryId)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price,
            CategoryId = categoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string description, decimal price, Guid categoryId)
    {
        Name = name;
        Description = description;
        Price = price;
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetImage(string? imagePath, string? telegramFileId)
    {
        ImagePath = imagePath;
        TelegramFileId = telegramFileId;
        UpdatedAt = DateTime.UtcNow;
    }
}
