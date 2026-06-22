namespace KitchenwareBot.Domain.Entities;

public class ProductDiscountTier
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public int MinQuantity { get; private set; }
    public int? MaxQuantity { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public bool IsActive { get; private set; }
    public int DisplayOrder { get; private set; }

    public Product Product { get; private set; } = default!;

    private ProductDiscountTier() { }

    public static ProductDiscountTier Create(Guid productId, int minQuantity, int? maxQuantity, decimal discountPercent, int displayOrder)
    {
        if (minQuantity < 0)
            throw new InvalidOperationException("حداقل تعداد نمی‌تواند منفی باشد.");
        if (maxQuantity.HasValue && maxQuantity < minQuantity)
            throw new InvalidOperationException("حداکثر تعداد نمی‌تواند از حداقل تعداد کمتر باشد.");
        if (discountPercent < 0 || discountPercent > 100)
            throw new InvalidOperationException("درصد تخفیف باید بین ۰ و ۱۰۰ باشد.");

        return new ProductDiscountTier
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            MinQuantity = minQuantity,
            MaxQuantity = maxQuantity,
            DiscountPercent = discountPercent,
            DisplayOrder = displayOrder,
            IsActive = true
        };
    }

    public void Update(int minQuantity, int? maxQuantity, decimal discountPercent, int displayOrder)
    {
        if (minQuantity < 0)
            throw new InvalidOperationException("حداقل تعداد نمی‌تواند منفی باشد.");
        if (maxQuantity.HasValue && maxQuantity < minQuantity)
            throw new InvalidOperationException("حداکثر تعداد نمی‌تواند از حداقل تعداد کمتر باشد.");
        if (discountPercent < 0 || discountPercent > 100)
            throw new InvalidOperationException("درصد تخفیف باید بین ۰ و ۱۰۰ باشد.");

        MinQuantity = minQuantity;
        MaxQuantity = maxQuantity;
        DiscountPercent = discountPercent;
        DisplayOrder = displayOrder;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
