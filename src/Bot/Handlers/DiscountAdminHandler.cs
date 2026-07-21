using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Application.Formatting;
using KitchenwareBot.Application.Messages;
using KitchenwareBot.Application.Services;
using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Bot.Common;
using KitchenwareBot.Bot.Keyboards;
using FluentValidation;

namespace KitchenwareBot.Bot.Handlers;

/// <summary>Manages global and per-product discount tiers. Both flows share the same
/// three text steps (min → max → percent); the draft's ProductId decides the target.</summary>
public class DiscountAdminHandler : HandlerBase
{
    private readonly IDiscountService _discounts;

    public DiscountAdminHandler(BotResponder bot, IDiscountService discounts) : base(bot) => _discounts = discounts;

    public async Task ShowMenuAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        ctx.Session.State = BotState.AdminDiscountMenu;
        await Answer(ctx, ct: ct);
        await Show(ctx, BotMessages.AdminDiscountMenu, AdminKeyboards.DiscountMenu(), ct);
    }

    // ── Global ────────────────────────────────────────────────────
    public async Task ShowGlobalTiersAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        var tiers = await _discounts.GetGlobalTiersAsync(ct);
        ctx.Session.DiscountDraft = null;
        ctx.Session.State = BotState.AdminGlobalDiscountList;
        await Answer(ctx, ct: ct);
        await Show(ctx, BotMessages.AdminBtnGlobalTiers, AdminKeyboards.GlobalTierList(tiers), ct);
    }

    public async Task StartAddGlobalAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        ctx.Session.DiscountDraft = new DiscountDraft { ProductId = null };
        ctx.Session.State = BotState.AdminGlobalDiscountAskMin;
        await Answer(ctx, ct: ct);
        await Show(ctx, BotMessages.AdminAskTierMin, null, ct);
    }

    public async Task DeleteGlobalAsync(BotUpdateContext ctx, Guid tierId, CancellationToken ct)
    {
        await _discounts.DeleteGlobalTierAsync(tierId, ct);
        await Answer(ctx, BotMessages.AdminTierDeleted, ct: ct);
        await ShowGlobalTiersAsync(ctx, ct);
    }

    // ── Per-product ───────────────────────────────────────────────
    public async Task ShowProductTiersAsync(BotUpdateContext ctx, Guid productId, CancellationToken ct)
    {
        var tiers = await _discounts.GetProductTiersAsync(productId, ct);
        ctx.Session.SelectedProductId = productId;
        ctx.Session.DiscountDraft = null;
        ctx.Session.State = BotState.AdminProductDiscountList;
        await Answer(ctx, ct: ct);
        var header = tiers.Count > 0 ? BotMessages.AdminBtnSetDiscount : BotMessages.NoDiscount;
        await Show(ctx, header, AdminKeyboards.ProductTierList(productId, tiers, tiers.Count > 0), ct);
    }

    public async Task StartAddProductAsync(BotUpdateContext ctx, Guid productId, CancellationToken ct)
    {
        ctx.Session.SelectedProductId = productId;
        ctx.Session.DiscountDraft = new DiscountDraft { ProductId = productId };
        ctx.Session.State = BotState.AdminGlobalDiscountAskMin;
        await Answer(ctx, ct: ct);
        await Show(ctx, BotMessages.AdminAskTierMin, null, ct);
    }

    public async Task DeleteProductTierAsync(BotUpdateContext ctx, Guid tierId, CancellationToken ct)
    {
        await _discounts.DeleteProductTierAsync(tierId, ct);
        await Answer(ctx, BotMessages.AdminTierDeleted, ct: ct);
        if (ctx.Session.SelectedProductId is { } pid) await ShowProductTiersAsync(ctx, pid, ct);
    }

    public async Task ClearProductTiersAsync(BotUpdateContext ctx, Guid productId, CancellationToken ct)
    {
        await _discounts.RemoveProductTiersAsync(productId, ct);
        await Answer(ctx, BotMessages.AdminProductTiersCleared, ct: ct);
        await ShowProductTiersAsync(ctx, productId, ct);
    }

    // ── Shared min → max → percent text flow ──────────────────────
    public async Task HandleTextAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        var draft = ctx.Session.DiscountDraft;
        if (draft is null) { ctx.Session.State = BotState.AdminMenu; return; }

        switch (ctx.Session.State)
        {
            case BotState.AdminGlobalDiscountAskMin:
                if (!PriceFormatter.TryParseInt(ctx.Text, out var min) || min < 1) { await Send(ctx, BotMessages.InvalidNumber, ct: ct); return; }
                draft.MinQty = min;
                ctx.Session.State = BotState.AdminGlobalDiscountAskMax;
                await Send(ctx, BotMessages.AdminAskTierMax, AdminKeyboards.Skip(Cb.AdminDiscSkipMax), ct);
                break;

            case BotState.AdminGlobalDiscountAskMax:
                if (!PriceFormatter.TryParseInt(ctx.Text, out var max) || max < draft.MinQty) { await Send(ctx, BotMessages.InvalidNumber, ct: ct); return; }
                draft.MaxQty = max;
                ctx.Session.State = BotState.AdminGlobalDiscountAskPercent;
                await Send(ctx, BotMessages.AdminAskTierPercent, ct: ct);
                break;

            case BotState.AdminGlobalDiscountAskPercent:
                if (!PriceFormatter.TryParseDecimal(ctx.Text, out var percent) || percent < 0 || percent > 100) { await Send(ctx, BotMessages.InvalidNumber, ct: ct); return; }
                draft.Percent = percent;
                await FinalizeAsync(ctx, ct);
                break;
        }
    }

    public async Task SkipMaxAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        var draft = ctx.Session.DiscountDraft;
        if (draft is null) { ctx.Session.State = BotState.AdminMenu; return; }
        draft.MaxQty = null;
        ctx.Session.State = BotState.AdminGlobalDiscountAskPercent;
        await Answer(ctx, ct: ct);
        await Show(ctx, BotMessages.AdminAskTierPercent, null, ct);
    }

    private async Task FinalizeAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        var draft = ctx.Session.DiscountDraft!;
        var input = new TierInputDto(draft.MinQty ?? 1, draft.MaxQty, draft.Percent ?? 0);
        try
        {
            if (draft.ProductId is { } productId)
            {
                await _discounts.AddProductTierAsync(productId, input, ct);
                ctx.Session.DiscountDraft = null;
                await Send(ctx, BotMessages.AdminTierSaved, ct: ct);
                await ShowProductTiersAsync(ctx, productId, ct);
            }
            else
            {
                await _discounts.AddGlobalTierAsync(input, ct);
                ctx.Session.DiscountDraft = null;
                await Send(ctx, BotMessages.AdminTierSaved, ct: ct);
                await ShowGlobalTiersAsync(ctx, ct);
            }
        }
        catch (ValidationException)
        {
            await Send(ctx, BotMessages.InvalidNumber, ct: ct);
        }
    }
}
