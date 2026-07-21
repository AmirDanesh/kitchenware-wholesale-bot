using KitchenwareBot.Domain.Enums;

namespace KitchenwareBot.Application.Sessions;

/// <summary>
/// Per-user conversation state, serialized to Redis as JSON (key <c>bot:session:{telegramId}</c>,
/// 30-minute TTL). Holds FSM position, the cart, and any in-progress draft. These are plain
/// POCOs (public setters) because they are ephemeral state, not business entities.
/// </summary>
public class UserSession
{
    public long TelegramId { get; set; }
    public BotState State { get; set; } = BotState.Idle;

    public List<CartItem> Cart { get; set; } = new();
    public OrderDraft? OrderDraft { get; set; }
    public ProductDraft? ProductDraft { get; set; }   // admin creating/editing a product
    public DiscountDraft? DiscountDraft { get; set; } // admin creating a discount tier

    // Contextual selections carried between steps
    public Guid? SelectedProductId { get; set; }
    public Guid? SelectedCategoryId { get; set; }
    public Guid? SelectedOrderId { get; set; }
    public Guid? SelectedTierId { get; set; }
    public Guid? SelectedWarehouseId { get; set; }
    public string? EditField { get; set; }          // which product field is being edited
    public OrderStatus? OrderStatusFilter { get; set; }
    public int CurrentPage { get; set; } = 1;

    /// <summary>Ad-hoc scratch storage for multi-step text collection (e.g. bank details).</summary>
    public Dictionary<string, string> Scratch { get; set; } = new();

    public void ResetToIdle()
    {
        State = BotState.Idle;
        OrderDraft = null;
        ProductDraft = null;
        DiscountDraft = null;
        SelectedProductId = null;
        SelectedCategoryId = null;
        SelectedOrderId = null;
        SelectedTierId = null;
        SelectedWarehouseId = null;
        EditField = null;
        OrderStatusFilter = null;
        CurrentPage = 1;
        Scratch.Clear();
    }

    public void ClearCart() => Cart.Clear();

    public decimal CartRawTotal => Cart.Sum(c => c.UnitPrice * c.Quantity);
    public int CartItemCount => Cart.Sum(c => c.Quantity);
}

/// <summary>A line in the in-memory cart. UnitPrice is the base price; the wholesale
/// discount is resolved live at view/checkout time from the current tiers.</summary>
public class CartItem
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; } // base unit price snapshot for display
    public int Quantity { get; set; }
}

public class OrderDraft
{
    public PaymentMethod? Payment { get; set; }
    public DeliveryType? Delivery { get; set; }
    public string? Address { get; set; }
}

public class ProductDraft
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public Guid? CategoryId { get; set; }
    public int? Stock { get; set; }
    public string? TelegramFileId { get; set; }
    public string? ImagePath { get; set; }
}

public class DiscountDraft
{
    public int? MinQty { get; set; }
    public int? MaxQty { get; set; }
    public decimal? Percent { get; set; }
    public Guid? ProductId { get; set; } // null = global tier
}
