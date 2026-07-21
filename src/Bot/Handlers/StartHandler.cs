using KitchenwareBot.Application.Messages;
using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Bot.Common;
using KitchenwareBot.Bot.Keyboards;

namespace KitchenwareBot.Bot.Handlers;

public class StartHandler : HandlerBase
{
    private readonly CatalogHandler _catalog;
    private readonly CartHandler _cart;

    public StartHandler(BotResponder bot, CatalogHandler catalog, CartHandler cart) : base(bot)
    {
        _catalog = catalog;
        _cart = cart;
    }

    /// <summary>Handles /start, including channel deep-link payloads
    /// (product_{id} → open product; buy_{id}_{qty} → add to cart).</summary>
    public async Task HandleStartAsync(BotUpdateContext ctx, string? payload, CancellationToken ct)
    {
        await SendWelcomeAsync(ctx, ct);

        if (string.IsNullOrWhiteSpace(payload)) return;

        if (payload.StartsWith("product_", StringComparison.Ordinal) &&
            Guid.TryParse(payload["product_".Length..], out var productId))
        {
            await _catalog.ShowProductDetailAsync(ctx, productId, ct);
            return;
        }

        if (payload.StartsWith("buy_", StringComparison.Ordinal))
        {
            var parts = payload.Split('_');
            if (parts.Length == 3 && Guid.TryParse(parts[1], out var buyId) && int.TryParse(parts[2], out var qty) && qty > 0)
                await _cart.AddViaDeeplinkAsync(ctx, buyId, qty, ct);
        }
    }

    public Task ShowMainMenuAsync(BotUpdateContext ctx, CancellationToken ct) => SendWelcomeAsync(ctx, ct);

    public async Task ShowHelpAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        ctx.Session.State = BotState.MainMenu;
        await Send(ctx, BotMessages.Help, CustomerKeyboards.MainMenu(), ct);
    }

    private Task SendWelcomeAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        var name = ctx.User.FirstName ?? ctx.User.Username ?? "کاربر";
        var text = string.Format(BotMessages.Welcome, name) + "\n\n" + BotMessages.MainMenu;
        ctx.Session.State = BotState.MainMenu;
        return Send(ctx, text, CustomerKeyboards.MainMenu(), ct);
    }
}
