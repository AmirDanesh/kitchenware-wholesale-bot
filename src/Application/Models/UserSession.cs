using KitchenwareBot.Domain.Enums;

namespace KitchenwareBot.Application.Models;

public class UserSession
{
    public long TelegramId { get; set; }
    public BotState State { get; set; } = BotState.Idle;
    public List<CartItem> Cart { get; set; } = new();
    public OrderDraft? OrderDraft { get; set; }
    public ProductDraft? ProductDraft { get; set; }
    public DiscountDraft? DiscountDraft { get; set; }
    public Guid? SelectedProductId { get; set; }
    public Guid? SelectedCategoryId { get; set; }
    public int CurrentPage { get; set; } = 1;
}

public class CartItem
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal UnitPrice { get; set; }
    public int Qty { get; set; }
}

public class OrderDraft
{
    public PaymentMethod Payment { get; set; }
    public DeliveryType Delivery { get; set; }
    public string? Address { get; set; }
}

public class ProductDraft
{
    public string? Name { get; set; }
    public string? Desc { get; set; }
    public decimal? Price { get; set; }
    public Guid? CategoryId { get; set; }
    public int? Stock { get; set; }
}

public class DiscountDraft
{
    public int? MinQty { get; set; }
    public int? MaxQty { get; set; }
    public decimal? Percent { get; set; }
    public Guid? ProductId { get; set; }
}
