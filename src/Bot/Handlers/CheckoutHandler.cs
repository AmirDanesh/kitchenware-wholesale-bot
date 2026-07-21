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

    public CheckoutHandler(BotResponder bot, IOrderService orders, IPaymentSettingsService payments) : base(bot)
    {
        _orders = orders;
        _payments = payments;
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

        ctx.Session.OrderDraft = new OrderDraft();
        ctx.Session.State = BotState.CheckoutAskDelivery;
        await Answer(ctx, ct: ct);
        await Show(ctx, BotMessages.AskDelivery, CustomerKeyboards.Delivery(), ct);
    }

    public async Task SetDeliveryAsync(BotUpdateContext ctx, DeliveryType delivery, CancellationToken ct)
    {
        ctx.Session.OrderDraft ??= new OrderDraft();
        ctx.Session.OrderDraft.Delivery = delivery;
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
        ctx.Session.OrderDraft ??= new OrderDraft();
        ctx.Session.OrderDraft.Address = ctx.Text.Trim();
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
        ctx.Session.OrderDraft ??= new OrderDraft();
        ctx.Session.OrderDraft.Payment = method;
        ctx.Session.State = BotState.CheckoutConfirm;
        await Answer(ctx, ct: ct);
        await ShowConfirmAsync(ctx, ct);
    }

    private async Task ShowConfirmAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        var calc = await _orders.CalculateOrderAsync(ctx.Session.Cart, ct);
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
        if (draft is null || ctx.Session.Cart.Count == 0)
        {
            await Answer(ctx, BotMessages.GenericError, alert: true, ct: ct);
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
            ctx.Session.State = BotState.Cart;
            await Send(ctx, $"{BotMessages.OutOfStock}\n🍳 {ex.ProductName}", CustomerKeyboards.MainMenu(), ct);
        }
        catch (ShopClosedException)
        {
            ctx.Session.ResetToIdle();
            await Send(ctx, BotMessages.ShopClosed, CustomerKeyboards.MainMenu(), ct);
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
}
