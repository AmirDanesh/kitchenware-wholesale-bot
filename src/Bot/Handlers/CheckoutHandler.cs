using System.Text;
using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Application.Formatting;
using KitchenwareBot.Application.Messages;
using KitchenwareBot.Application.Services;
using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Bot.Common;
using KitchenwareBot.Bot.Keyboards;
using KitchenwareBot.Domain.Enums;
using KitchenwareBot.Domain.Exceptions;

namespace KitchenwareBot.Bot.Handlers;

public class CheckoutHandler : HandlerBase
{
    private readonly IOrderService _orders;
    private readonly IPaymentSettingsService _payments;
    private readonly IBotStateService _state;

    public CheckoutHandler(BotResponder bot, IOrderService orders, IPaymentSettingsService payments,
        IBotStateService state) : base(bot)
    {
        _orders = orders;
        _payments = payments;
        _state = state;
    }

    public async Task StartAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        if (ctx.Session.Cart.Count == 0)
        {
            await Answer(ctx, BotMessages.CartEmpty, alert: true, ct: ct);
            return;
        }
        var settings = await _payments.GetAsync(ct);
        if (!settings.IsShopOpen)
        {
            await Answer(ctx, ct: ct);
            await Show(ctx, BotMessages.ShopClosed, null, ct);
            return;
        }

        ctx.Session.OrderDraft = CreateDraft();
        ctx.Session.State = BotState.CheckoutAskDelivery;
        await Answer(ctx, ct: ct);
        await Show(ctx, BotMessages.AskDelivery, CustomerKeyboards.Delivery(), ct);
    }

    public async Task SetDeliveryAsync(BotUpdateContext ctx, DeliveryType delivery, CancellationToken ct)
    {
        var draft = GetOrCreateDraft(ctx.Session);
        draft.Delivery = delivery;
        await Answer(ctx, ct: ct);

        if (delivery == DeliveryType.Shipping)
        {
            ctx.Session.State = BotState.CheckoutAskAddress;
            await Show(ctx, BotMessages.AskAddress, null, ct);
        }
        else
        {
            await AskPaymentAsync(ctx, ct);
        }
    }

    public async Task HandleAddressAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ctx.Text))
        {
            await Send(ctx, BotMessages.AskAddress, ct: ct);
            return;
        }
        var draft = GetOrCreateDraft(ctx.Session);
        draft.Address = ctx.Text.Trim();
        await AskPaymentAsync(ctx, ct);
    }

    private async Task AskPaymentAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        var settings = await _payments.GetAsync(ct);
        ctx.Session.State = BotState.CheckoutAskPayment;
        await Show(ctx, BotMessages.AskPayment,
            CustomerKeyboards.Payment(settings.BankTransferEnabled, settings.CashEnabled), ct);
    }

    public async Task SetPaymentAsync(BotUpdateContext ctx, PaymentMethod method, CancellationToken ct)
    {
        var draft = GetOrCreateDraft(ctx.Session);
        draft.Payment = method;
        ctx.Session.State = BotState.CheckoutConfirm;
        await Answer(ctx, ct: ct);
        await ShowConfirmAsync(ctx, ct);
    }

    private async Task ShowConfirmAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        OrderCalculationDto calc;
        try
        {
            calc = await _orders.CalculateOrderAsync(ctx.Session.Cart, ct);
        }
        catch (ProductUnavailableException ex)
        {
            ctx.Session.State = BotState.Cart;
            await Show(ctx, string.Format(BotMessages.ProductUnavailable, ex.ProductName),
                CustomerKeyboards.Cart(ctx.Session.Cart), ct);
            return;
        }

        // Keep the session cart aligned with the exact current prices shown to the customer.
        foreach (var line in calc.Lines)
        {
            var cartItem = ctx.Session.Cart.First(i => i.ProductId == line.ProductId);
            cartItem.Name = line.ProductName;
            cartItem.UnitPrice = line.OriginalUnitPrice;
        }

        var draft = ctx.Session.OrderDraft!;

        var sb = new StringBuilder();
        sb.AppendLine(BotMessages.ConfirmOrderPrompt);
        sb.AppendLine();
        sb.AppendLine(CartHandler.BuildCartText(calc));
        sb.AppendLine();
        if (draft.Delivery is { } d)
        {
            sb.AppendLine($"🚚 تحویل: {BotMessages.DeliveryLabel(d)}");
            if (d == DeliveryType.Shipping && !string.IsNullOrWhiteSpace(draft.Address))
                sb.AppendLine($"📍 آدرس: {draft.Address}");
        }
        if (draft.Payment is { } p)
            sb.AppendLine($"💳 پرداخت: {BotMessages.PaymentLabel(p)}");

        await Show(ctx, sb.ToString().TrimEnd(), CustomerKeyboards.ConfirmOrder(), ct);
    }

    public async Task ConfirmAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        var draft = ctx.Session.OrderDraft;
        if (draft is null || draft.CheckoutToken == Guid.Empty || ctx.Session.Cart.Count == 0)
        {
            ctx.Session.State = BotState.Cart;
            await Answer(ctx, BotMessages.CheckoutExpired, alert: true, ct: ct);
            return;
        }

        if (!await _state.TryBeginCheckoutAsync(ctx.TelegramId, draft.CheckoutToken, ct))
        {
            // Another request owns this checkout. Never let its stale session overwrite the winner.
            ctx.PersistSession = false;
            await Answer(ctx, BotMessages.CheckoutAlreadyProcessing, alert: true, ct: ct);
            return;
        }

        var customerName = ctx.User.FirstName ?? ctx.User.Username ?? "مشتری";
        await Answer(ctx, ct: ct);
        try
        {
            var result = await _orders.PlaceOrderAsync(ctx.TelegramId, customerName, ctx.User.Phone,
                ctx.Session.Cart, draft, ct);

            ctx.Session.ClearCart();
            ctx.Session.ResetToIdle();

            var message = string.Format(BotMessages.OrderPlaced, result.ShortCode);
            if (result.BankDetails is not null)
                message += "\n\n" + BuildBankInstructions(result);

            await Send(ctx, message, CustomerKeyboards.MainMenu(), ct);
        }
        catch (InsufficientStockException ex)
        {
            await _state.ReleaseCheckoutAsync(ctx.TelegramId, draft.CheckoutToken, ct);
            ctx.Session.State = BotState.Cart;
            await Send(ctx, $"{BotMessages.OutOfStock}\n🍳 {ex.ProductName}", CustomerKeyboards.MainMenu(), ct);
        }
        catch (ProductUnavailableException ex)
        {
            await _state.ReleaseCheckoutAsync(ctx.TelegramId, draft.CheckoutToken, ct);
            ctx.Session.State = BotState.Cart;
            await Send(ctx, string.Format(BotMessages.ProductUnavailable, ex.ProductName), CustomerKeyboards.MainMenu(), ct);
        }
        catch (ShopClosedException)
        {
            await _state.ReleaseCheckoutAsync(ctx.TelegramId, draft.CheckoutToken, ct);
            ctx.Session.ResetToIdle();
            await Send(ctx, BotMessages.ShopClosed, CustomerKeyboards.MainMenu(), ct);
        }
        catch
        {
            await _state.ReleaseCheckoutAsync(ctx.TelegramId, draft.CheckoutToken, ct);
            throw;
        }
    }

    public async Task CancelAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        ctx.Session.OrderDraft = null;
        ctx.Session.State = BotState.Idle;
        await Answer(ctx, BotMessages.OperationCancelled, ct: ct);
        await Send(ctx, BotMessages.OperationCancelled, CustomerKeyboards.MainMenu(), ct);
    }

    private static string BuildBankInstructions(PlaceOrderResultDto result)
    {
        var b = result.BankDetails!;
        var sb = new StringBuilder();
        sb.AppendLine(BotMessages.BankInstructionsHeader);
        if (!string.IsNullOrWhiteSpace(b.BankName)) sb.AppendLine($"بانک: {b.BankName}");
        if (!string.IsNullOrWhiteSpace(b.AccountNumber)) sb.AppendLine($"شماره کارت/حساب: {b.AccountNumber}");
        if (!string.IsNullOrWhiteSpace(b.AccountName)) sb.AppendLine($"به نام: {b.AccountName}");
        sb.AppendLine($"مبلغ: {PriceFormatter.FormatToman(result.Total)}");
        if (!string.IsNullOrWhiteSpace(b.Note)) sb.AppendLine(b.Note);
        return sb.ToString().TrimEnd();
    }

    private static OrderDraft CreateDraft() => new() { CheckoutToken = Guid.NewGuid() };

    private static OrderDraft GetOrCreateDraft(UserSession session)
    {
        session.OrderDraft ??= CreateDraft();
        if (session.OrderDraft.CheckoutToken == Guid.Empty)
            session.OrderDraft.CheckoutToken = Guid.NewGuid();
        return session.OrderDraft;
    }
}
