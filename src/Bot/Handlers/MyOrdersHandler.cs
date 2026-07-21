using System.Text;
using KitchenwareBot.Application.Common;
using KitchenwareBot.Application.Formatting;
using KitchenwareBot.Application.Messages;
using KitchenwareBot.Application.Services;
using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Bot.Common;
using KitchenwareBot.Bot.Keyboards;
using KitchenwareBot.Domain.Entities;

namespace KitchenwareBot.Bot.Handlers;

public class MyOrdersHandler : HandlerBase
{
    private readonly IOrderService _orders;

    public MyOrdersHandler(BotResponder bot, IOrderService orders) : base(bot) => _orders = orders;

    public async Task ShowOrdersAsync(BotUpdateContext ctx, int page, CancellationToken ct)
    {
        var result = await _orders.GetCustomerOrdersAsync(ctx.TelegramId, page, Paging.DefaultPageSize, ct);
        ctx.Session.State = BotState.MyOrders;
        ctx.Session.CurrentPage = page;

        if (result.TotalCount == 0)
        {
            await Show(ctx, BotMessages.NoOrders, null, ct);
            return;
        }
        await Show(ctx, BotMessages.MyOrdersHeader, CustomerKeyboards.Orders(result.Items, page, result.TotalPages), ct);
    }

    public async Task ShowOrderDetailAsync(BotUpdateContext ctx, Guid orderId, CancellationToken ct)
    {
        var order = await _orders.GetOrderAsync(orderId, ct);
        if (order is null || order.CustomerTelegramId != ctx.TelegramId)
        {
            await Answer(ctx, BotMessages.NothingHere, alert: true, ct: ct);
            return;
        }
        await Answer(ctx, ct: ct);
        await Show(ctx, BuildOrderText(order), CustomerKeyboards.OrderDetailBack(), ct);
    }

    public static string BuildOrderText(Order order)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🧾 سفارش {order.ShortCode}");
        sb.AppendLine($"📅 {PersianDate.Format(order.CreatedAt)}");
        sb.AppendLine($"وضعیت: {BotMessages.OrderStatusLabel(order.Status)}");
        sb.AppendLine("— اقلام —");
        foreach (var item in order.Items)
        {
            sb.AppendLine($"🍳 {item.ProductName} × {PriceFormatter.FormatNumber(item.Quantity)}");
            if (item.DiscountPercent > 0)
                sb.AppendLine($"  ({PriceFormatter.FormatPercent(item.DiscountPercent)} تخفیف) {PriceFormatter.FormatToman(item.SubTotal)}");
            else
                sb.AppendLine($"  {PriceFormatter.FormatToman(item.SubTotal)}");
        }
        sb.AppendLine($"💳 جمع کل: {PriceFormatter.FormatToman(order.TotalAmount)}");
        if (!string.IsNullOrWhiteSpace(order.AdminNote))
            sb.AppendLine($"📝 {order.AdminNote}");
        return sb.ToString().TrimEnd();
    }
}
