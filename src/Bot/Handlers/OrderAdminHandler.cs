using System.Text;
using KitchenwareBot.Application.Common;
using KitchenwareBot.Application.Formatting;
using KitchenwareBot.Application.Messages;
using KitchenwareBot.Application.Services;
using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Bot.Common;
using KitchenwareBot.Bot.Keyboards;
using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Enums;
using KitchenwareBot.Domain.Exceptions;

namespace KitchenwareBot.Bot.Handlers;

public class OrderAdminHandler : HandlerBase
{
    private readonly IOrderService _orders;

    public OrderAdminHandler(BotResponder bot, IOrderService orders) : base(bot) => _orders = orders;

    public async Task ShowListAsync(BotUpdateContext ctx, OrderStatus? status, int page, CancellationToken ct)
    {
        var result = await _orders.GetAllOrdersAsync(status, page, Paging.DefaultPageSize, ct);
        ctx.Session.State = BotState.AdminOrderList;
        ctx.Session.OrderStatusFilter = status;
        var statusArg = status.HasValue ? ((int)status.Value).ToString() : "-";
        await Answer(ctx, ct: ct);
        var text = result.TotalCount == 0 ? BotMessages.NothingHere : BotMessages.AdminOrdersHeader;
        await Show(ctx, text, AdminKeyboards.OrderList(result.Items, statusArg, page, result.TotalPages), ct);
    }

    public async Task ShowOrderAsync(BotUpdateContext ctx, Guid orderId, CancellationToken ct)
    {
        var order = await _orders.GetOrderAsync(orderId, ct);
        if (order is null)
        {
            await Answer(ctx, BotMessages.NothingHere, alert: true, ct: ct);
            return;
        }
        ctx.Session.SelectedOrderId = orderId;
        ctx.Session.State = BotState.AdminViewingOrder;
        await Answer(ctx, ct: ct);
        await Show(ctx, BuildAdminOrderText(order), AdminKeyboards.OrderActions(order), ct);
    }

    public async Task ChangeStatusAsync(BotUpdateContext ctx, Guid orderId, OrderStatus target, CancellationToken ct)
    {
        // Shipped/Cancelled usually carry a note (tracking number / reason).
        if (target is OrderStatus.Shipped or OrderStatus.Cancelled)
        {
            ctx.Session.SelectedOrderId = orderId;
            ctx.Session.EditField = ((int)target).ToString(); // stash target status for the text step
            ctx.Session.State = BotState.AdminOrderAskNote;
            await Answer(ctx, ct: ct);
            await Show(ctx, BotMessages.AdminAskOrderNote,
                AdminKeyboards.Skip(Cb.Make(Cb.AdminOrderNoteSkip, orderId, (int)target)), ct);
            return;
        }
        await Answer(ctx, ct: ct);
        await ApplyStatusAsync(ctx, orderId, target, null, ct);
    }

    public async Task HandleNoteTextAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        if (ctx.Session.SelectedOrderId is not { } orderId ||
            !int.TryParse(ctx.Session.EditField, out var statusInt))
        {
            ctx.Session.State = BotState.AdminMenu;
            return;
        }
        var note = string.IsNullOrWhiteSpace(ctx.Text) ? null : ctx.Text.Trim();
        await ApplyStatusAsync(ctx, orderId, (OrderStatus)statusInt, note, ct);
    }

    public async Task SkipNoteAsync(BotUpdateContext ctx, Guid orderId, OrderStatus target, CancellationToken ct)
    {
        await Answer(ctx, ct: ct);
        await ApplyStatusAsync(ctx, orderId, target, null, ct);
    }

    private async Task ApplyStatusAsync(BotUpdateContext ctx, Guid orderId, OrderStatus target, string? note, CancellationToken ct)
    {
        ctx.Session.EditField = null;
        try
        {
            await _orders.UpdateOrderStatusAsync(orderId, target, note, ct);
            await Send(ctx, BotMessages.AdminOrderStatusUpdated, ct: ct);
        }
        catch (DomainException)
        {
            await Send(ctx, BotMessages.GenericError, ct: ct);
        }
        ctx.Session.State = BotState.AdminViewingOrder;
        await ShowOrderAsync(ctx, orderId, ct);
    }

    private static string BuildAdminOrderText(Order order)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🧾 سفارش {order.ShortCode}");
        sb.AppendLine($"📅 {PersianDate.Format(order.CreatedAt)}");
        sb.AppendLine($"وضعیت: {BotMessages.OrderStatusLabel(order.Status)}");
        sb.AppendLine($"👤 مشتری: {order.CustomerName}");
        if (!string.IsNullOrWhiteSpace(order.CustomerPhone)) sb.AppendLine($"📞 {order.CustomerPhone}");
        sb.AppendLine($"💳 پرداخت: {BotMessages.PaymentLabel(order.PaymentMethod)}");
        sb.AppendLine($"🚚 تحویل: {BotMessages.DeliveryLabel(order.DeliveryType)}");
        if (order.DeliveryType == DeliveryType.Shipping && !string.IsNullOrWhiteSpace(order.ShippingAddress))
            sb.AppendLine($"📍 {order.ShippingAddress}");
        sb.AppendLine("— اقلام —");
        foreach (var item in order.Items)
        {
            var disc = item.DiscountPercent > 0 ? $" ({PriceFormatter.FormatPercent(item.DiscountPercent)}-)" : string.Empty;
            sb.AppendLine($"🍳 {item.ProductName} × {PriceFormatter.FormatNumber(item.Quantity)}{disc} = {PriceFormatter.FormatToman(item.SubTotal)}");
        }
        sb.AppendLine($"💰 جمع کل: {PriceFormatter.FormatToman(order.TotalAmount)}");
        if (!string.IsNullOrWhiteSpace(order.AdminNote)) sb.AppendLine($"📝 {order.AdminNote}");
        return sb.ToString().TrimEnd();
    }
}
