using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Application.Messages;
using KitchenwareBot.Application.Services;
using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Bot.Common;
using KitchenwareBot.Bot.Configuration;
using KitchenwareBot.Bot.Keyboards;

namespace KitchenwareBot.Bot.Handlers;

public class SettingsAdminHandler : HandlerBase
{
    private readonly IPaymentSettingsService _payments;
    private readonly RuntimeBotSettings _runtime;

    public SettingsAdminHandler(BotResponder bot, IPaymentSettingsService payments, RuntimeBotSettings runtime) : base(bot)
    {
        _payments = payments;
        _runtime = runtime;
    }

    public async Task ShowMenuAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        ctx.Session.State = BotState.AdminSettings;
        await Answer(ctx, ct: ct);
        await Show(ctx, BotMessages.AdminSettingsMenu, AdminKeyboards.SettingsMenu(), ct);
    }

    public async Task ShowPaymentAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        var settings = await _payments.GetAsync(ct);
        ctx.Session.State = BotState.AdminPaymentSettings;
        var status = settings.IsShopOpen ? BotMessages.ShopStatusOpen : BotMessages.ShopStatusClosed;
        await Answer(ctx, ct: ct);
        await Show(ctx, $"{BotMessages.AdminBtnPaymentSettings}\n{status}",
            AdminKeyboards.PaymentSettings(settings.BankTransferEnabled, settings.CashEnabled), ct);
    }

    public async Task ToggleBankAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        var settings = await _payments.GetAsync(ct);
        await _payments.SetBankTransferEnabledAsync(!settings.BankTransferEnabled, ct);
        await ShowPaymentAsync(ctx, ct);
    }

    public async Task ToggleCashAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        var settings = await _payments.GetAsync(ct);
        await _payments.SetCashEnabledAsync(!settings.CashEnabled, ct);
        await ShowPaymentAsync(ctx, ct);
    }

    // ── Bank details wizard (name → number → holder → note) ───────
    public async Task StartEditBankAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        ctx.Session.Scratch.Clear();
        ctx.Session.State = BotState.AdminBankDetailsAskName;
        await Answer(ctx, ct: ct);
        await Show(ctx, BotMessages.AdminAskBankName, null, ct);
    }

    public async Task StartSetChannelAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        ctx.Session.State = BotState.AdminSettingsAskChannel;
        await Answer(ctx, ct: ct);
        await Show(ctx, BotMessages.AdminAskChannelId, null, ct);
    }

    public async Task SkipBankNoteAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        await Answer(ctx, ct: ct);
        await SaveBankDetailsAsync(ctx, note: null, ct);
    }

    public async Task HandleTextAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        var text = ctx.Text?.Trim() ?? string.Empty;
        switch (ctx.Session.State)
        {
            case BotState.AdminBankDetailsAskName:
                ctx.Session.Scratch["bankName"] = text;
                ctx.Session.State = BotState.AdminBankDetailsAskNumber;
                await Send(ctx, BotMessages.AdminAskBankNumber, ct: ct);
                break;

            case BotState.AdminBankDetailsAskNumber:
                ctx.Session.Scratch["bankNumber"] = text;
                ctx.Session.State = BotState.AdminBankDetailsAskHolder;
                await Send(ctx, BotMessages.AdminAskBankHolder, ct: ct);
                break;

            case BotState.AdminBankDetailsAskHolder:
                ctx.Session.Scratch["bankHolder"] = text;
                ctx.Session.State = BotState.AdminBankDetailsAskNote;
                await Send(ctx, BotMessages.AdminAskBankNote, AdminKeyboards.Skip(Cb.AdminSkipBankNote), ct);
                break;

            case BotState.AdminBankDetailsAskNote:
                await SaveBankDetailsAsync(ctx, string.IsNullOrWhiteSpace(text) ? null : text, ct);
                break;

            case BotState.AdminSettingsAskChannel:
                _runtime.ChannelId = string.IsNullOrWhiteSpace(text) ? null : text;
                ctx.Session.State = BotState.AdminSettings;
                await Send(ctx, BotMessages.AdminSettingsSaved, ct: ct);
                await ShowMenuAsync(ctx, ct);
                break;
        }
    }

    private async Task SaveBankDetailsAsync(BotUpdateContext ctx, string? note, CancellationToken ct)
    {
        var s = ctx.Session.Scratch;
        var dto = new BankDetailsDto(
            s.GetValueOrDefault("bankName"),
            s.GetValueOrDefault("bankNumber"),
            s.GetValueOrDefault("bankHolder"),
            note);
        await _payments.UpdateBankDetailsAsync(dto, ct);
        ctx.Session.Scratch.Clear();
        ctx.Session.State = BotState.AdminPaymentSettings;
        await Send(ctx, BotMessages.AdminSettingsSaved, ct: ct);
        await ShowPaymentAsync(ctx, ct);
    }
}
