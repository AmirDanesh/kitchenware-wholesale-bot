using KitchenwareBot.Application.Messages;
using KitchenwareBot.Application.Services;
using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Bot.Common;
using KitchenwareBot.Bot.Handlers;
using KitchenwareBot.Domain.Enums;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace KitchenwareBot.Bot.Routing;

/// <summary>Routes an incoming Telegram Update to the right handler based on commands,
/// main-menu buttons, callback data, and the user's FSM state.</summary>
public class UpdateRouter
{
    private readonly IUserService _users;
    private readonly IBotStateService _state;
    private readonly BotResponder _responder;
    private readonly ILogger<UpdateRouter> _logger;

    private readonly StartHandler _start;
    private readonly CatalogHandler _catalog;
    private readonly CartHandler _cart;
    private readonly CheckoutHandler _checkout;
    private readonly MyOrdersHandler _myOrders;
    private readonly AdminMenuHandler _adminMenu;
    private readonly ProductAdminHandler _productAdmin;
    private readonly ChannelPublishHandler _channel;
    private readonly OrderAdminHandler _orderAdmin;
    private readonly InventoryAdminHandler _inventoryAdmin;
    private readonly DiscountAdminHandler _discountAdmin;
    private readonly SettingsAdminHandler _settingsAdmin;

    public UpdateRouter(
        IUserService users, IBotStateService state, BotResponder responder, ILogger<UpdateRouter> logger,
        StartHandler start, CatalogHandler catalog, CartHandler cart, CheckoutHandler checkout, MyOrdersHandler myOrders,
        AdminMenuHandler adminMenu, ProductAdminHandler productAdmin, ChannelPublishHandler channel,
        OrderAdminHandler orderAdmin, InventoryAdminHandler inventoryAdmin, DiscountAdminHandler discountAdmin,
        SettingsAdminHandler settingsAdmin)
    {
        _users = users; _state = state; _responder = responder; _logger = logger;
        _start = start; _catalog = catalog; _cart = cart; _checkout = checkout; _myOrders = myOrders;
        _adminMenu = adminMenu; _productAdmin = productAdmin; _channel = channel;
        _orderAdmin = orderAdmin; _inventoryAdmin = inventoryAdmin; _discountAdmin = discountAdmin; _settingsAdmin = settingsAdmin;
    }

    public async Task RouteAsync(Update update, CancellationToken ct)
    {
        var message = update.Message;
        var callback = update.CallbackQuery;
        var from = message?.From ?? callback?.From;
        if (from is null) return;

        long telegramId = from.Id;
        long chatId = message?.Chat.Id ?? callback?.Message?.Chat.Id ?? telegramId;

        var user = await _users.GetOrCreateAsync(telegramId, from.Username, from.FirstName, ct);
        if (user.IsBanned)
        {
            if (callback is not null) await _responder.AnswerAsync(callback.Id, BotMessages.Banned, true, ct);
            else await _responder.SendAsync(chatId, BotMessages.Banned, null, ct);
            return;
        }

        var isAdmin = await _users.IsAdminAsync(telegramId, ct);
        var session = await _state.GetOrCreateAsync(telegramId, ct);

        var ctx = new BotUpdateContext
        {
            Update = update,
            TelegramId = telegramId,
            ChatId = chatId,
            User = user,
            Session = session,
            IsAdmin = isAdmin,
            Message = message,
            Text = message?.Text?.Trim(),
            Callback = callback,
            MessageId = callback?.Message?.MessageId,
            Args = callback?.Data?.Split(':') ?? Array.Empty<string>()
        };

        try
        {
            if (ctx.IsCallback) await DispatchCallbackAsync(ctx, ct);
            else await DispatchMessageAsync(ctx, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update for {TelegramId}", telegramId);
            try { await _responder.SendAsync(chatId, BotMessages.GenericError, null, ct); } catch { /* ignore */ }
        }

        // Stop the callback spinner if a handler didn't already answer.
        if (ctx.IsCallback && !ctx.CallbackAnswered && callback is not null)
            await _responder.AnswerAsync(callback.Id, ct: ct);

        await _state.SetAsync(session, ct);
    }

    // ── Messages (text / photo) ───────────────────────────────────
    private async Task DispatchMessageAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        // Photo during the product-image step.
        if (ctx.Message?.Photo is { Length: > 0 } photos && ctx.IsAdmin && ctx.Session.State == BotState.AdminProductAskImage)
        {
            await _productAdmin.HandlePhotoAsync(ctx, photos[^1].FileId, ct);
            return;
        }

        var text = ctx.Text;
        if (string.IsNullOrWhiteSpace(text)) return;

        if (text.StartsWith('/'))
        {
            await DispatchCommandAsync(ctx, text, ct);
            return;
        }

        // Main-menu reply-keyboard buttons.
        switch (text)
        {
            case BotMessages.BtnCatalog: await _catalog.ShowCategoriesAsync(ctx, ct); return;
            case BotMessages.BtnCart: await _cart.ShowCartAsync(ctx, ct); return;
            case BotMessages.BtnMyOrders: await _myOrders.ShowOrdersAsync(ctx, 1, ct); return;
            case BotMessages.BtnHelp: await _start.ShowHelpAsync(ctx, ct); return;
        }

        await DispatchTextByStateAsync(ctx, ct);
    }

    private async Task DispatchCommandAsync(BotUpdateContext ctx, string text, CancellationToken ct)
    {
        var spaceIdx = text.IndexOf(' ');
        var command = (spaceIdx < 0 ? text : text[..spaceIdx]).ToLowerInvariant();
        var payload = spaceIdx < 0 ? null : text[(spaceIdx + 1)..].Trim();
        var atIdx = command.IndexOf('@');
        if (atIdx >= 0) command = command[..atIdx]; // strip @botusername

        switch (command)
        {
            case "/start": await _start.HandleStartAsync(ctx, payload, ct); break;
            case "/admin":
                if (ctx.IsAdmin) await _adminMenu.ShowMenuAsync(ctx, ct);
                else await _start.ShowMainMenuAsync(ctx, ct);
                break;
            case "/help": await _start.ShowHelpAsync(ctx, ct); break;
            case "/cancel":
                ctx.Session.ResetToIdle();
                await _start.ShowMainMenuAsync(ctx, ct);
                break;
            default: await _start.ShowMainMenuAsync(ctx, ct); break;
        }
    }

    private async Task DispatchTextByStateAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        switch (ctx.Session.State)
        {
            case BotState.ViewingProductAskQty:
                await _cart.HandleCustomQtyAsync(ctx, ct); break;
            case BotState.CheckoutAskAddress:
                await _checkout.HandleAddressAsync(ctx, ct); break;

            case BotState.AdminProductAskName:
            case BotState.AdminProductAskDescription:
            case BotState.AdminProductAskPrice:
            case BotState.AdminProductAskStock:
            case BotState.AdminProductEditAskValue:
                await _productAdmin.HandleTextAsync(ctx, ct); break;

            case BotState.AdminOrderAskNote:
                await _orderAdmin.HandleNoteTextAsync(ctx, ct); break;

            case BotState.AdminAdjustStockAskQty:
                await _inventoryAdmin.HandleTextAsync(ctx, ct); break;

            case BotState.AdminGlobalDiscountAskMin:
            case BotState.AdminGlobalDiscountAskMax:
            case BotState.AdminGlobalDiscountAskPercent:
                await _discountAdmin.HandleTextAsync(ctx, ct); break;

            case BotState.AdminBankDetailsAskName:
            case BotState.AdminBankDetailsAskNumber:
            case BotState.AdminBankDetailsAskHolder:
            case BotState.AdminBankDetailsAskNote:
            case BotState.AdminSettingsAskChannel:
                await _settingsAdmin.HandleTextAsync(ctx, ct); break;

            default:
                // Unrecognized free text — gently return to the main menu.
                await _start.ShowMainMenuAsync(ctx, ct);
                break;
        }
    }

    // ── Callbacks ─────────────────────────────────────────────────
    private async Task DispatchCallbackAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        var token = ctx.Arg(0);
        if (token == Cb.Admin) { await DispatchAdminCallbackAsync(ctx, ct); return; }

        switch (token)
        {
            case Cb.Menu: await _start.ShowMainMenuAsync(ctx, ct); break;
            case Cb.Cats: await _catalog.ShowCategoriesAsync(ctx, ct); break;
            case Cb.Cat: await _catalog.ShowProductsAsync(ctx, ParseGuid(ctx.Arg(1)), 1, ct); break;
            case Cb.ProdPage: await _catalog.ShowProductsAsync(ctx, ParseNullableGuid(ctx.Arg(1)), ParseInt(ctx.Arg(2), 1), ct); break;
            case Cb.Prod: await _catalog.ShowProductDetailAsync(ctx, ParseGuid(ctx.Arg(1)), ct); break;
            case Cb.AddCart: await _cart.AddToCartAsync(ctx, ParseGuid(ctx.Arg(1)), ParseInt(ctx.Arg(2), 0), ct); break;
            case Cb.AskQty: await _cart.PromptCustomQtyAsync(ctx, ParseGuid(ctx.Arg(1)), ct); break;
            case Cb.Cart: await _cart.ShowCartAsync(ctx, ct); break;
            case Cb.CartClear: await _cart.ClearAsync(ctx, ct); break;
            case Cb.CartDel: await _cart.RemoveAsync(ctx, ParseGuid(ctx.Arg(1)), ct); break;
            case Cb.Checkout: await _checkout.StartAsync(ctx, ct); break;
            case Cb.Delivery: await _checkout.SetDeliveryAsync(ctx, (DeliveryType)ParseInt(ctx.Arg(1), 0), ct); break;
            case Cb.Pay: await _checkout.SetPaymentAsync(ctx, (PaymentMethod)ParseInt(ctx.Arg(1), 0), ct); break;
            case Cb.Confirm: await _checkout.ConfirmAsync(ctx, ct); break;
            case Cb.CheckoutCancel: await _checkout.CancelAsync(ctx, ct); break;
            case Cb.Orders: await _myOrders.ShowOrdersAsync(ctx, ParseInt(ctx.Arg(1), 1), ct); break;
            case Cb.OrderView: await _myOrders.ShowOrderDetailAsync(ctx, ParseGuid(ctx.Arg(1)), ct); break;
            case Cb.Noop: await _responder.AnswerAsync(ctx.Callback!.Id, ct: ct); ctx.CallbackAnswered = true; break;
            default: await _responder.AnswerAsync(ctx.Callback!.Id, ct: ct); ctx.CallbackAnswered = true; break;
        }
    }

    private async Task DispatchAdminCallbackAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        if (!ctx.IsAdmin)
        {
            await _responder.AnswerAsync(ctx.Callback!.Id, BotMessages.Unauthorized, true, ct);
            ctx.CallbackAnswered = true;
            return;
        }

        var section = ctx.Arg(1);
        switch (section)
        {
            case "menu": await _adminMenu.ShowMenuAsync(ctx, ct); break;

            // Products
            case "prods": await _productAdmin.ShowListAsync(ctx, ParseInt(ctx.Arg(2), 1), ct); break;
            case "prod": await _productAdmin.ShowProductAsync(ctx, ParseGuid(ctx.Arg(2)), ct); break;
            case "padd": await _productAdmin.StartAddAsync(ctx, ct); break;
            case "pdel": await _productAdmin.DeleteAsync(ctx, ParseGuid(ctx.Arg(2)), ctx.Args.Length > 3 && ctx.Arg(3) == "1", ct); break;
            case "ptog": await _productAdmin.ToggleActiveAsync(ctx, ParseGuid(ctx.Arg(2)), ct); break;
            case "ppub": await _channel.PublishAsync(ctx, ParseGuid(ctx.Arg(2)), ct); break;
            case "pdisc": await _discountAdmin.ShowProductTiersAsync(ctx, ParseGuid(ctx.Arg(2)), ct); break;
            case "ped": await _productAdmin.ShowEditMenuAsync(ctx, ParseGuid(ctx.Arg(2)), ct); break;
            case "pef": await _productAdmin.StartEditFieldAsync(ctx, ctx.Arg(2), ct); break;
            case "pac": await _productAdmin.HandleAddCategoryAsync(ctx, ParseGuid(ctx.Arg(2)), ct); break;
            case "pec": await _productAdmin.HandleEditCategoryAsync(ctx, ParseGuid(ctx.Arg(2)), ct); break;
            case "pski": await _productAdmin.SkipImageAsync(ctx, ct); break;

            // Orders
            case "ords": await _orderAdmin.ShowListAsync(ctx, ParseNullableStatus(ctx.Arg(2)), ParseInt(ctx.Arg(3), 1), ct); break;
            case "ord": await _orderAdmin.ShowOrderAsync(ctx, ParseGuid(ctx.Arg(2)), ct); break;
            case "ost": await _orderAdmin.ChangeStatusAsync(ctx, ParseGuid(ctx.Arg(2)), (OrderStatus)ParseInt(ctx.Arg(3), 0), ct); break;
            case "ons": await _orderAdmin.SkipNoteAsync(ctx, ParseGuid(ctx.Arg(2)), (OrderStatus)ParseInt(ctx.Arg(3), 0), ct); break;

            // Inventory
            case "inv": await _inventoryAdmin.ShowMenuAsync(ctx, ct); break;
            case "invr": await _inventoryAdmin.ShowStockReportAsync(ctx, ct); break;
            case "invl": await _inventoryAdmin.ShowLowStockAsync(ctx, ct); break;
            case "inva": await _inventoryAdmin.StartAdjustAsync(ctx, ct); break;
            case "invap": await _inventoryAdmin.PickProductAsync(ctx, ParseGuid(ctx.Arg(2)), ct); break;
            case "invaw": await _inventoryAdmin.PickWarehouseAsync(ctx, ParseGuid(ctx.Arg(2)), ct); break;

            // Discounts
            case "disc": await _discountAdmin.ShowMenuAsync(ctx, ct); break;
            case "dg": await _discountAdmin.ShowGlobalTiersAsync(ctx, ct); break;
            case "dga": await _discountAdmin.StartAddGlobalAsync(ctx, ct); break;
            case "dgd": await _discountAdmin.DeleteGlobalAsync(ctx, ParseGuid(ctx.Arg(2)), ct); break;
            case "dpa": await _discountAdmin.StartAddProductAsync(ctx, ParseGuid(ctx.Arg(2)), ct); break;
            case "dpd": await _discountAdmin.DeleteProductTierAsync(ctx, ParseGuid(ctx.Arg(2)), ct); break;
            case "dpc": await _discountAdmin.ClearProductTiersAsync(ctx, ParseGuid(ctx.Arg(2)), ct); break;
            case "dmax": await _discountAdmin.SkipMaxAsync(ctx, ct); break;

            // Settings
            case "set": await _settingsAdmin.ShowMenuAsync(ctx, ct); break;
            case "setp": await _settingsAdmin.ShowPaymentAsync(ctx, ct); break;
            case "tbank": await _settingsAdmin.ToggleBankAsync(ctx, ct); break;
            case "tcash": await _settingsAdmin.ToggleCashAsync(ctx, ct); break;
            case "ebank": await _settingsAdmin.StartEditBankAsync(ctx, ct); break;
            case "sbn": await _settingsAdmin.SkipBankNoteAsync(ctx, ct); break;
            case "chan": await _settingsAdmin.StartSetChannelAsync(ctx, ct); break;

            default: await _responder.AnswerAsync(ctx.Callback!.Id, ct: ct); ctx.CallbackAnswered = true; break;
        }
    }

    // ── Parse helpers ─────────────────────────────────────────────
    private static Guid ParseGuid(string s) => Guid.TryParse(s, out var g) ? g : Guid.Empty;
    private static Guid? ParseNullableGuid(string s) => Guid.TryParse(s, out var g) ? g : null;
    private static int ParseInt(string s, int def) => int.TryParse(s, out var n) ? n : def;
    private static OrderStatus? ParseNullableStatus(string s)
        => s != "-" && int.TryParse(s, out var n) ? (OrderStatus)n : null;
}
