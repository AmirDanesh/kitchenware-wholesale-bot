using KitchenwareBot.Application.Formatting;
using KitchenwareBot.Application.Messages;
using KitchenwareBot.Bot.Common;
using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace KitchenwareBot.Bot.Keyboards;

public static class AdminKeyboards
{
    public static InlineKeyboardMarkup AdminMain() =>
        new(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnProducts, Cb.AdminProducts + ":1") },
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnOrders, Cb.AdminOrders + ":-:1") },
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnInventory, Cb.AdminInventory) },
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnDiscounts, Cb.AdminDiscounts) },
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnSettings, Cb.AdminSettings) }
        });

    public static InlineKeyboardMarkup ProductList(IReadOnlyList<Product> products, int page, int totalPages)
    {
        var rows = products
            .Select(p => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{(p.IsActive ? "🟢" : "⚪️")} {p.Name} — {PriceFormatter.FormatToman(p.Price)}",
                    Cb.Make(Cb.AdminProduct, p.Id))
            })
            .ToList();

        CustomerKeyboards.AddSimplePagination(rows, Cb.AdminProducts, page, totalPages);
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnAddProduct, Cb.AdminProductAdd) });
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnBack, Cb.AdminMenu) });
        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup ProductActions(Guid productId, bool isActive) =>
        new(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnEdit, Cb.Make(Cb.AdminProductEdit, productId)),
                InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnSetDiscount, Cb.Make(Cb.AdminProductDiscount, productId))
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnPublish, Cb.Make(Cb.AdminProductPublish, productId)),
                InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnToggleActive, Cb.Make(Cb.AdminProductToggle, productId))
            },
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnDelete, Cb.Make(Cb.AdminProductDel, productId)) },
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnBack, Cb.AdminProducts + ":1") }
        });

    public static InlineKeyboardMarkup EditFieldMenu() =>
        new(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("نام", Cb.Make(Cb.AdminProductEditField, "name")) },
            new[] { InlineKeyboardButton.WithCallbackData("توضیحات", Cb.Make(Cb.AdminProductEditField, "desc")) },
            new[] { InlineKeyboardButton.WithCallbackData("قیمت", Cb.Make(Cb.AdminProductEditField, "price")) },
            new[] { InlineKeyboardButton.WithCallbackData("دسته‌بندی", Cb.Make(Cb.AdminProductEditField, "cat")) }
        });

    /// <summary>Category picker. <paramref name="token"/> is Cb.AdminProductAddCategory or Cb.AdminProductEditCategory.</summary>
    public static InlineKeyboardMarkup CategoryPicker(IReadOnlyList<Category> categories, string token)
    {
        var rows = categories
            .Select(c => new[] { InlineKeyboardButton.WithCallbackData(c.Name, Cb.Make(token, c.Id)) })
            .ToList();
        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup Skip(string skipCallback) =>
        new(new[] { new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnSkip, skipCallback) } });

    public static InlineKeyboardMarkup YesNo(string yesCallback, string noCallback) =>
        new(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(BotMessages.BtnYes, yesCallback),
                InlineKeyboardButton.WithCallbackData(BotMessages.BtnNo, noCallback)
            }
        });

    // ── Orders ────────────────────────────────────────────────────
    public static InlineKeyboardMarkup OrderList(IReadOnlyList<Order> orders, string statusArg, int page, int totalPages)
    {
        var rows = orders
            .Select(o => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{o.ShortCode} — {BotMessages.OrderStatusLabel(o.Status)} — {PriceFormatter.FormatToman(o.TotalAmount)}",
                    Cb.Make(Cb.AdminOrder, o.Id))
            })
            .ToList();

        // Status filter row
        rows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnFilterAll, Cb.AdminOrders + ":-:1"),
            InlineKeyboardButton.WithCallbackData(BotMessages.OrderStatusLabel(OrderStatus.Pending), Cb.Make(Cb.AdminOrders, (int)OrderStatus.Pending, 1))
        });

        if (totalPages > 1)
        {
            var nav = new List<InlineKeyboardButton>();
            if (page > 1) nav.Add(InlineKeyboardButton.WithCallbackData(BotMessages.BtnPrevPage, Cb.Make(Cb.AdminOrders, statusArg, page - 1)));
            nav.Add(InlineKeyboardButton.WithCallbackData(PriceFormatter.ToPersianDigits($"{page}/{totalPages}"), Cb.Noop));
            if (page < totalPages) nav.Add(InlineKeyboardButton.WithCallbackData(BotMessages.BtnNextPage, Cb.Make(Cb.AdminOrders, statusArg, page + 1)));
            rows.Add(nav.ToArray());
        }

        rows.Add(new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnBack, Cb.AdminMenu) });
        return new InlineKeyboardMarkup(rows);
    }

    /// <summary>Buttons to advance the order to its next valid status, plus Cancel.</summary>
    public static InlineKeyboardMarkup OrderActions(Order order)
    {
        var rows = new List<InlineKeyboardButton[]>();
        var next = NextStatus(order.Status);
        if (next.HasValue)
            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"➡️ {BotMessages.OrderStatusLabel(next.Value)}", Cb.Make(Cb.AdminOrderStatus, order.Id, (int)next.Value))
            });

        if (order.Status is not (OrderStatus.Delivered or OrderStatus.Cancelled))
            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"❌ {BotMessages.OrderStatusLabel(OrderStatus.Cancelled)}", Cb.Make(Cb.AdminOrderStatus, order.Id, (int)OrderStatus.Cancelled))
            });

        rows.Add(new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnBack, Cb.AdminOrders + ":-:1") });
        return new InlineKeyboardMarkup(rows);
    }

    public static OrderStatus? NextStatus(OrderStatus status) => status switch
    {
        OrderStatus.Pending => OrderStatus.Confirmed,
        OrderStatus.Confirmed => OrderStatus.Processing,
        OrderStatus.Processing => OrderStatus.Shipped,
        OrderStatus.Shipped => OrderStatus.Delivered,
        _ => null
    };

    // ── Inventory ─────────────────────────────────────────────────
    public static InlineKeyboardMarkup InventoryMenu() =>
        new(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnStockReport, Cb.AdminInvReport) },
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnLowStock, Cb.AdminInvLow) },
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnAdjustStock, Cb.AdminInvAdjust) },
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnBack, Cb.AdminMenu) }
        });

    public static InlineKeyboardMarkup ProductPicker(IReadOnlyList<Product> products, string token)
    {
        var rows = products
            .Select(p => new[] { InlineKeyboardButton.WithCallbackData(p.Name, Cb.Make(token, p.Id)) })
            .ToList();
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnBack, Cb.AdminInventory) });
        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup WarehousePicker(IReadOnlyList<Warehouse> warehouses)
    {
        var rows = warehouses
            .Select(w => new[] { InlineKeyboardButton.WithCallbackData(w.Name, Cb.Make(Cb.AdminInvAdjustWarehouse, w.Id)) })
            .ToList();
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnBack, Cb.AdminInventory) });
        return new InlineKeyboardMarkup(rows);
    }

    // ── Discounts ─────────────────────────────────────────────────
    public static InlineKeyboardMarkup DiscountMenu() =>
        new(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnGlobalTiers, Cb.AdminDiscGlobal) },
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnBack, Cb.AdminMenu) }
        });

    public static InlineKeyboardMarkup GlobalTierList(IReadOnlyList<GlobalDiscountTier> tiers)
    {
        var rows = tiers
            .Select(t => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"🗑 {TierLabel(t.MinQuantity, t.MaxQuantity, t.DiscountPercent)}", Cb.Make(Cb.AdminDiscGlobalDel, t.Id))
            })
            .ToList();
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnAddTier, Cb.AdminDiscGlobalAdd) });
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnBack, Cb.AdminDiscounts) });
        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup ProductTierList(Guid productId, IReadOnlyList<ProductDiscountTier> tiers, bool hasProductTiers)
    {
        var rows = tiers
            .Select(t => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"🗑 {TierLabel(t.MinQuantity, t.MaxQuantity, t.DiscountPercent)}", Cb.Make(Cb.AdminDiscProductDel, t.Id))
            })
            .ToList();
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnAddTier, Cb.Make(Cb.AdminDiscProductAdd, productId)) });
        if (hasProductTiers)
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnUseGlobal, Cb.Make(Cb.AdminDiscProductClear, productId)) });
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnBack, Cb.Make(Cb.AdminProduct, productId)) });
        return new InlineKeyboardMarkup(rows);
    }

    // ── Settings ──────────────────────────────────────────────────
    public static InlineKeyboardMarkup SettingsMenu() =>
        new(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnPaymentSettings, Cb.AdminSetPayment) },
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnChannelSettings, Cb.AdminSetChannel) },
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnBack, Cb.AdminMenu) }
        });

    public static InlineKeyboardMarkup PaymentSettings(bool bankEnabled, bool cashEnabled) =>
        new(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData($"{BotMessages.AdminBtnToggleBank}: {(bankEnabled ? BotMessages.Enabled : BotMessages.Disabled)}", Cb.AdminToggleBank) },
            new[] { InlineKeyboardButton.WithCallbackData($"{BotMessages.AdminBtnToggleCash}: {(cashEnabled ? BotMessages.Enabled : BotMessages.Disabled)}", Cb.AdminToggleCash) },
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.AdminBtnEditBankDetails, Cb.AdminEditBank) },
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnBack, Cb.AdminSettings) }
        });

    private static string TierLabel(int min, int? max, decimal percent)
    {
        var range = max.HasValue
            ? $"{PriceFormatter.FormatNumber(min)}–{PriceFormatter.FormatNumber(max.Value)}"
            : $"{PriceFormatter.FormatNumber(min)}+";
        return $"{range}: {PriceFormatter.FormatPercent(percent)}";
    }
}
