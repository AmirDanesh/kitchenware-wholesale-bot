using KitchenwareBot.Domain.Common;

namespace KitchenwareBot.Domain.Entities;

/// <summary>A wholesale quantity-discount tier that applies to all products unless a
/// product defines its own tiers.</summary>
public class GlobalDiscountTier : BaseEntity
{
    public int MinQuantity { get; private set; }
    public int? MaxQuantity { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int DisplayOrder { get; private set; }

    private GlobalDiscountTier() { }

    public static GlobalDiscountTier Create(int minQuantity, int? maxQuantity, decimal discountPercent, int displayOrder = 0)
    {
        Validate(minQuantity, maxQuantity, discountPercent);
        return new GlobalDiscountTier
        {
            MinQuantity = minQuantity,
            MaxQuantity = maxQuantity,
            DiscountPercent = discountPercent,
            DisplayOrder = displayOrder,
            IsActive = true
        };
    }

    public void Update(int minQuantity, int? maxQuantity, decimal discountPercent)
    {
        Validate(minQuantity, maxQuantity, discountPercent);
        MinQuantity = minQuantity;
        MaxQuantity = maxQuantity;
        DiscountPercent = discountPercent;
    }

    public void SetActive(bool active) => IsActive = active;
    public void SetDisplayOrder(int order) => DisplayOrder = order;

    /// <summary>True when the given order quantity falls within this tier's bounds.</summary>
    public bool Matches(int quantity)
        => quantity >= MinQuantity && (MaxQuantity is null || quantity <= MaxQuantity);

    internal static void Validate(int minQuantity, int? maxQuantity, decimal discountPercent)
    {
        if (minQuantity < 1)
            throw new ArgumentOutOfRangeException(nameof(minQuantity), "Minimum quantity must be at least 1.");
        if (maxQuantity is not null && maxQuantity < minQuantity)
            throw new ArgumentException("Maximum quantity cannot be less than minimum quantity.");
        if (discountPercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(discountPercent), "Discount percent must be between 0 and 100.");
    }
}
