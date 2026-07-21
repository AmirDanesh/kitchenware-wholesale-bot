using System.Text;
using KitchenwareBot.Application.Abstractions;
using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Application.Formatting;
using KitchenwareBot.Application.Messages;
using KitchenwareBot.Bot.Configuration;
using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace KitchenwareBot.Bot.Notifications;

/// <summary>Telegram implementation of the Application notification abstraction.
/// Every send is guarded so a failed notification never breaks the calling operation.</summary>
public class TelegramNotificationService : INotificationService
{
    private readonly ITelegramBotClient _bot;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramNotificationService> _logger;

    public TelegramNotificationService(ITelegramBotClient bot, IOptions<TelegramOptions> options,
        ILogger<TelegramNotificationService> logger)
    {
        _bot = bot;
        _options = options.Value;
        _logger = logger;
    }

    public async Task NotifyAdminsNewOrderAsync(Order order, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BotMessages.AdminNewOrderHeader);
        sb.AppendLine($"کد سفارش: {order.ShortCode}");
        sb.AppendLine($"مشتری: {order.CustomerName}");
        if (!string.IsNullOrWhiteSpace(order.CustomerPhone))
            sb.AppendLine($"تلفن: {order.CustomerPhone}");
        sb.AppendLine($"پرداخت: {BotMessages.PaymentLabel(order.PaymentMethod)}");
        sb.AppendLine($"تحویل: {BotMessages.DeliveryLabel(order.DeliveryType)}");
        if (order.DeliveryType == DeliveryType.Shipping && !string.IsNullOrWhiteSpace(order.ShippingAddress))
            sb.AppendLine($"آدرس: {order.ShippingAddress}");
        sb.AppendLine("— اقلام —");
        foreach (var item in order.Items)
            sb.AppendLine($"• {item.ProductName} × {PriceFormatter.FormatNumber(item.Quantity)} = {PriceFormatter.FormatToman(item.SubTotal)}");
        sb.AppendLine($"جمع کل: {PriceFormatter.FormatToman(order.TotalAmount)}");

        await BroadcastToAdminsAsync(sb.ToString(), ct);
    }

    public async Task NotifyAdminsLowStockAsync(LowStockItemDto item, CancellationToken ct = default)
    {
        var text = $"{BotMessages.AdminLowStockHeader}\n" +
                   $"{item.ProductName} — انبار {item.WarehouseName}\n" +
                   $"موجودی قابل فروش: {PriceFormatter.FormatNumber(item.Available)}";
        await BroadcastToAdminsAsync(text, ct);
    }

    public async Task NotifyCustomerOrderStatusAsync(Order order, CancellationToken ct = default)
    {
        var text = BotMessages.StatusNotification(order.Status);
        if (!string.IsNullOrWhiteSpace(order.AdminNote) &&
            order.Status is OrderStatus.Shipped or OrderStatus.Cancelled)
        {
            text += $"\n{order.AdminNote}";
        }
        text += $"\nکد سفارش: {order.ShortCode}";

        try
        {
            await _bot.SendMessage(order.CustomerTelegramId, text, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify customer {TelegramId} about order {OrderId}",
                order.CustomerTelegramId, order.Id);
        }
    }

    private async Task BroadcastToAdminsAsync(string text, CancellationToken ct)
    {
        foreach (var adminId in _options.AdminIds ?? Array.Empty<long>())
        {
            try
            {
                await _bot.SendMessage(adminId, text, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify admin {AdminId}", adminId);
            }
        }
    }
}
