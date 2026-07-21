using KitchenwareBot.Domain.Common;

namespace KitchenwareBot.Domain.Entities;

/// <summary>A quantity-discount tier specific to one product. If a product has any
/// active tiers, they completely replace the global tiers for that product.</summary>
public class ProductDiscountTier : BaseEntity
{
    public Guid ProductId { get; private set; }
    public int MinQuantity { get; private set; }
    public int? MaxQuantity { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int DisplayOrder { get; private set; }

    // Navigation
    public Product? Product { get; private set; }

    private ProductDiscountTier() { }

    public static ProductDiscountTier Create(Guid productId, int minQuantity, int? maxQuantity, decimal discountPercent, int displayOrder = 0)
    {
        GlobalDiscountTier.Validate(minQuantity, maxQuantity, discountPercent);
        return new ProductDiscountTier
        {
            ProductId = productId,
            MinQuantity = minQuantity,
            MaxQuantity = maxQuantity,
            DiscountPercent = discountPercent,
            DisplayOrder = displayOrder,
            IsActive = true
        };
    }

    public void Update(int minQuantity, int? maxQuantity, decimal discountPercent)
    {
        GlobalDiscountTier.Validate(minQuantity, maxQuantity, discountPercent);
        MinQuantity = minQuantity;
        MaxQuantity = maxQuantity;
        DiscountPercent = discountPercent;
    }

    public void SetActive(bool active) => IsActive = active;
    public void SetDisplayOrder(int order) => DisplayOrder = order;

    public bool Matches(int quantity)
        => quantity >= MinQuantity && (MaxQuantity is null || quantity <= MaxQuantity);
}
