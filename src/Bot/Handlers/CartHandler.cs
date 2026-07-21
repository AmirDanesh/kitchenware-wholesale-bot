using System.Text;
using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Application.Formatting;
using KitchenwareBot.Application.Messages;
using KitchenwareBot.Application.Services;
using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Bot.Common;
using KitchenwareBot.Bot.Keyboards;

namespace KitchenwareBot.Bot.Handlers;

public class CartHandler : HandlerBase
{
    private readonly IProductService _products;
    private readonly IOrderService _orders;

    public CartHandler(BotResponder bot, IProductService products, IOrderService orders) : base(bot)
    {
        _products = products;
        _orders = orders;
    }

    /// <summary>Prompt the customer to type a custom quantity for the selected product.</summary>
    public async Task PromptCustomQtyAsync(BotUpdateContext ctx, Guid productId, CancellationToken ct)
    {
        ctx.Session.SelectedProductId = productId;
        ctx.Session.State = BotState.ViewingProductAskQty;
        await Answer(ctx, ct: ct);
        await Send(ctx, BotMessages.AskCustomQty, ct: ct);
    }

    public async Task AddToCartAsync(BotUpdateContext ctx, Guid productId, int qty, CancellationToken ct)
    {
        var added = await TryAddAsync(ctx, productId, qty, ct);
        await Answer(ctx, added ?? BotMessages.AddedToCart, alert: added is not null, ct: ct);
    }

    /// <summary>Handles a typed custom quantity for the currently-selected product.</summary>
    public async Task HandleCustomQtyAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        if (ctx.Session.SelectedProductId is not { } productId)
        {
            ctx.Session.State = BotState.Idle;
            return;
        }
        if (!PriceFormatter.TryParseInt(ctx.Text, out var qty) || qty <= 0)
        {
            await Send(ctx, BotMessages.InvalidNumber, ct: ct);
            return;
        }

        var error = await TryAddAsync(ctx, productId, qty, ct);
        ctx.Session.State = BotState.Cart;
        if (error is not null)
        {
            await Send(ctx, error, ct: ct);
            return;
        }
        await Send(ctx, BotMessages.AddedToCart, ct: ct);
        await ShowCartAsync(ctx, ct);
    }

    /// <summary>Adds to cart; returns null on success or an error message for a toast.</summary>
    private async Task<string?> TryAddAsync(BotUpdateContext ctx, Guid productId, int qty, CancellationToken ct)
    {
        if (qty <= 0) return BotMessages.InvalidNumber;

        var dto = await _products.GetProductDetailAsync(productId, ct);
        if (dto is null || !dto.IsActive) return BotMessages.NothingHere;

        var existing = ctx.Session.Cart.FirstOrDefault(c => c.ProductId == productId);
        var currentQty = existing?.Quantity ?? 0;
        if (currentQty + qty > dto.AvailableStock) return BotMessages.OutOfStock;

        if (existing is not null)
            existing.Quantity += qty;
        else
            ctx.Session.Cart.Add(new CartItem { ProductId = productId, Name = dto.Name, UnitPrice = dto.Price, Quantity = qty });

        return null;
    }

    /// <summary>Add-to-cart from a channel deep link (start=buy_{id}_{qty}).</summary>
    public async Task AddViaDeeplinkAsync(BotUpdateContext ctx, Guid productId, int qty, CancellationToken ct)
    {
        var error = await TryAddAsync(ctx, productId, qty, ct);
        if (error is not null)
        {
            await Send(ctx, error, CustomerKeyboards.MainMenu(), ct);
            return;
        }
        await Send(ctx, BotMessages.AddedToCart, ct: ct);
        await ShowCartAsync(ctx, ct);
    }

    public async Task ShowCartAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        ctx.Session.State = BotState.Cart;
        if (ctx.Session.Cart.Count == 0)
        {
            await Show(ctx, BotMessages.CartEmpty, CustomerKeyboards.Cart(ctx.Session.Cart), ct);
            return;
        }

        var calc = await _orders.CalculateOrderAsync(ctx.Session.Cart, ct);
        await Show(ctx, BuildCartText(calc), CustomerKeyboards.Cart(ctx.Session.Cart), ct);
    }

    public async Task RemoveAsync(BotUpdateContext ctx, Guid productId, CancellationToken ct)
    {
        ctx.Session.Cart.RemoveAll(c => c.ProductId == productId);
        await Answer(ctx, ct: ct);
        await ShowCartAsync(ctx, ct);
    }

    public async Task ClearAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        ctx.Session.ClearCart();
        await Answer(ctx, BotMessages.CartCleared, ct: ct);
        await ShowCartAsync(ctx, ct);
    }

    public static string BuildCartText(OrderCalculationDto calc)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BotMessages.CartHeader);
        sb.AppendLine();
        foreach (var line in calc.Lines)
        {
            sb.AppendLine($"🍳 {line.ProductName} × {PriceFormatter.FormatNumber(line.Quantity)}");
            sb.AppendLine($"قیمت واحد: {PriceFormatter.FormatToman(line.OriginalUnitPrice)}");
            if (line.DiscountPercent > 0)
                sb.AppendLine($"تخفیف: {PriceFormatter.FormatPercent(line.DiscountPercent)} ({PriceFormatter.FormatToman(line.Saved)})");
            sb.AppendLine($"جمع: {PriceFormatter.FormatToman(line.LineTotal)}");
            sb.AppendLine("➖➖➖");
        }
        if (calc.TotalSaved > 0)
            sb.AppendLine($"مجموع تخفیف: {PriceFormatter.FormatToman(calc.TotalSaved)}");
        sb.AppendLine($"💳 مبلغ قابل پرداخت: {PriceFormatter.FormatToman(calc.GrandTotal)}");
        return sb.ToString().TrimEnd();
    }
}
