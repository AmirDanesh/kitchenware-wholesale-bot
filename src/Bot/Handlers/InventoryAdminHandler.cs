using System.Text;
using KitchenwareBot.Application.Formatting;
using KitchenwareBot.Application.Messages;
using KitchenwareBot.Application.Services;
using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Bot.Common;
using KitchenwareBot.Bot.Keyboards;

namespace KitchenwareBot.Bot.Handlers;

public class InventoryAdminHandler : HandlerBase
{
    private readonly IInventoryService _inventory;
    private readonly IProductService _products;

    public InventoryAdminHandler(BotResponder bot, IInventoryService inventory, IProductService products) : base(bot)
    {
        _inventory = inventory;
        _products = products;
    }

    public async Task ShowMenuAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        ctx.Session.State = BotState.AdminInventoryMenu;
        await Answer(ctx, ct: ct);
        await Show(ctx, BotMessages.AdminInventoryMenu, AdminKeyboards.InventoryMenu(), ct);
    }

    public async Task ShowStockReportAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        var report = await _inventory.GetStockReportAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine(BotMessages.AdminBtnStockReport);
        if (report.Count == 0) sb.AppendLine(BotMessages.NothingHere);
        foreach (var r in report)
            sb.AppendLine($"{(r.IsLowStock ? "⚠️" : "•")} {r.ProductName}: {PriceFormatter.FormatNumber(r.TotalAvailable)}");
        await Answer(ctx, ct: ct);
        await Show(ctx, sb.ToString().TrimEnd(), AdminKeyboards.InventoryMenu(), ct);
    }

    public async Task ShowLowStockAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        var items = await _inventory.GetLowStockAlertsAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine(BotMessages.AdminBtnLowStock);
        if (items.Count == 0) sb.AppendLine(BotMessages.AdminNoLowStock);
        foreach (var i in items)
            sb.AppendLine($"⚠️ {i.ProductName} ({i.WarehouseName}): {PriceFormatter.FormatNumber(i.Available)}");
        await Answer(ctx, ct: ct);
        await Show(ctx, sb.ToString().TrimEnd(), AdminKeyboards.InventoryMenu(), ct);
    }

    public async Task StartAdjustAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        var products = await _products.GetAllProductsAsync(1, 50, ct);
        ctx.Session.State = BotState.AdminAdjustStockAskProduct;
        await Answer(ctx, ct: ct);
        await Show(ctx, BotMessages.AdminAskAdjustProduct,
            AdminKeyboards.ProductPicker(products.Items, Cb.AdminInvAdjustProduct), ct);
    }

    public async Task PickProductAsync(BotUpdateContext ctx, Guid productId, CancellationToken ct)
    {
        ctx.Session.SelectedProductId = productId;
        var warehouses = await _inventory.GetWarehousesAsync(ct);
        ctx.Session.State = BotState.AdminAdjustStockAskWarehouse;
        await Answer(ctx, ct: ct);
        await Show(ctx, BotMessages.AdminAskAdjustWarehouse, AdminKeyboards.WarehousePicker(warehouses), ct);
    }

    public async Task PickWarehouseAsync(BotUpdateContext ctx, Guid warehouseId, CancellationToken ct)
    {
        ctx.Session.SelectedWarehouseId = warehouseId;
        ctx.Session.State = BotState.AdminAdjustStockAskQty;
        await Answer(ctx, ct: ct);
        await Show(ctx, BotMessages.AdminAskAdjustQty, null, ct);
    }

    public async Task HandleTextAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        if (ctx.Session.SelectedProductId is not { } productId ||
            ctx.Session.SelectedWarehouseId is not { } warehouseId)
        {
            ctx.Session.State = BotState.AdminMenu;
            return;
        }
        if (!PriceFormatter.TryParseInt(ctx.Text, out var delta) || delta == 0)
        {
            await Send(ctx, BotMessages.InvalidNumber, ct: ct);
            return;
        }

        try
        {
            await _inventory.AdjustStockAsync(productId, warehouseId, delta, null, ct);
            await Send(ctx, BotMessages.AdminStockAdjusted, ct: ct);
        }
        catch (Exception)
        {
            await Send(ctx, BotMessages.GenericError, ct: ct);
        }

        ctx.Session.SelectedWarehouseId = null;
        ctx.Session.State = BotState.AdminInventoryMenu;
        await ShowMenuAsync(ctx, ct);
    }
}
