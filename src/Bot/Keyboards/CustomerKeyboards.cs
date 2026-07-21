using KitchenwareBot.Application.Formatting;
using KitchenwareBot.Application.Messages;
using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Bot.Common;
using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace KitchenwareBot.Bot.Keyboards;

public static class CustomerKeyboards
{
    public static readonly int[] PresetQuantities = { 1, 2, 5, 10, 20 };

    public static ReplyKeyboardMarkup MainMenu() =>
        new(new[]
        {
            new KeyboardButton[] { BotMessages.BtnCatalog, BotMessages.BtnCart },
            new KeyboardButton[] { BotMessages.BtnMyOrders, BotMessages.BtnHelp }
        })
        { ResizeKeyboard = true };

    public static InlineKeyboardMarkup Categories(IReadOnlyList<Category> categories)
    {
        var rows = categories
            .Select(c => new[] { InlineKeyboardButton.WithCallbackData(c.Name, Cb.Make(Cb.Cat, c.Id)) })
            .ToList();
        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup Products(IReadOnlyList<Product> products, Guid? categoryId, int page, int totalPages)
    {
        var rows = products
            .Select(p => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{p.Name} — {PriceFormatter.FormatToman(p.Price)}", Cb.Make(Cb.Prod, p.Id))
            })
            .ToList();

        AddPagination(rows, Cb.ProdPage, page, totalPages, categoryId?.ToString() ?? "-");
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnBack, Cb.Cats) });
        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup ProductDetail(Guid productId, Guid categoryId, bool canOrder)
    {
        var rows = new List<InlineKeyboardButton[]>();
        if (canOrder)
        {
            // Rows of preset-quantity "add" buttons, 3 per row.
            var qtyButtons = PresetQuantities
                .Select(q => InlineKeyboardButton.WithCallbackData(
                    $"📦 {PriceFormatter.FormatNumber(q)}", Cb.Make(Cb.AddCart, productId, q)))
                .ToList();
            foreach (var chunk in Chunk(qtyButtons, 3))
                rows.Add(chunk.ToArray());
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnCustomQty, Cb.Make(Cb.AskQty, productId)) });
        }
        rows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(BotMessages.BtnCart, Cb.Cart),
            InlineKeyboardButton.WithCallbackData(BotMessages.BtnBack, Cb.Make(Cb.ProdPage, categoryId, 1))
        });
        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup Cart(IReadOnlyList<CartItem> items)
    {
        var rows = new List<InlineKeyboardButton[]>();
        foreach (var item in items)
            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData($"🗑 {item.Name}", Cb.Make(Cb.CartDel, item.ProductId))
            });

        if (items.Count > 0)
        {
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnCheckout, Cb.Checkout) });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnClearCart, Cb.CartClear) });
        }
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnCatalog, Cb.Cats) });
        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup Delivery() =>
        new(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnDeliveryShipping, Cb.Make(Cb.Delivery, (int)DeliveryType.Shipping)) },
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnDeliveryInPerson, Cb.Make(Cb.Delivery, (int)DeliveryType.InPerson)) },
            new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnCancel, Cb.CheckoutCancel) }
        });

    public static InlineKeyboardMarkup Payment(bool bankEnabled, bool cashEnabled)
    {
        var rows = new List<InlineKeyboardButton[]>();
        if (bankEnabled)
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnPayBank, Cb.Make(Cb.Pay, (int)PaymentMethod.BankTransfer)) });
        if (cashEnabled)
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnPayCash, Cb.Make(Cb.Pay, (int)PaymentMethod.Cash)) });
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnCancel, Cb.CheckoutCancel) });
        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup ConfirmOrder() =>
        new(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(BotMessages.BtnConfirm, Cb.Confirm),
                InlineKeyboardButton.WithCallbackData(BotMessages.BtnCancel, Cb.CheckoutCancel)
            }
        });

    public static InlineKeyboardMarkup Orders(IReadOnlyList<Order> orders, int page, int totalPages)
    {
        var rows = orders
            .Select(o => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{o.ShortCode} — {BotMessages.OrderStatusLabel(o.Status)}", Cb.Make(Cb.OrderView, o.Id))
            })
            .ToList();

        AddSimplePagination(rows, Cb.Orders, page, totalPages);
        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup OrderDetailBack() =>
        new(new[] { new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BtnBack, Cb.Make(Cb.Orders, 1)) } });

    // ── helpers ──────────────────────────────────────────────────
    internal static void AddPagination(List<InlineKeyboardButton[]> rows, string token, int page, int totalPages, string arg)
    {
        if (totalPages <= 1) return;
        var nav = new List<InlineKeyboardButton>();
        if (page > 1) nav.Add(InlineKeyboardButton.WithCallbackData(BotMessages.BtnPrevPage, Cb.Make(token, arg, page - 1)));
        nav.Add(InlineKeyboardButton.WithCallbackData(PriceFormatter.ToPersianDigits($"{page}/{totalPages}"), Cb.Noop));
        if (page < totalPages) nav.Add(InlineKeyboardButton.WithCallbackData(BotMessages.BtnNextPage, Cb.Make(token, arg, page + 1)));
        rows.Add(nav.ToArray());
    }

    internal static void AddSimplePagination(List<InlineKeyboardButton[]> rows, string token, int page, int totalPages)
    {
        if (totalPages <= 1) return;
        var nav = new List<InlineKeyboardButton>();
        if (page > 1) nav.Add(InlineKeyboardButton.WithCallbackData(BotMessages.BtnPrevPage, Cb.Make(token, page - 1)));
        nav.Add(InlineKeyboardButton.WithCallbackData(PriceFormatter.ToPersianDigits($"{page}/{totalPages}"), Cb.Noop));
        if (page < totalPages) nav.Add(InlineKeyboardButton.WithCallbackData(BotMessages.BtnNextPage, Cb.Make(token, page + 1)));
        rows.Add(nav.ToArray());
    }

    private static IEnumerable<List<T>> Chunk<T>(IReadOnlyList<T> source, int size)
    {
        for (var i = 0; i < source.Count; i += size)
            yield return source.Skip(i).Take(size).ToList();
    }
}
