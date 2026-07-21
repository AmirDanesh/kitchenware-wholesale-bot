using System.Text;
using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Application.Formatting;
using KitchenwareBot.Application.Messages;
using KitchenwareBot.Application.Services;
using KitchenwareBot.Bot.Common;
using KitchenwareBot.Bot.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace KitchenwareBot.Bot.Handlers;

public class ChannelPublishHandler : HandlerBase
{
    private readonly IProductService _products;
    private readonly ITelegramBotClient _client;
    private readonly TelegramOptions _options;
    private readonly RuntimeBotSettings _runtime;
    private readonly ILogger<ChannelPublishHandler> _logger;

    public ChannelPublishHandler(BotResponder bot, IProductService products, ITelegramBotClient client,
        IOptions<TelegramOptions> options, RuntimeBotSettings runtime, ILogger<ChannelPublishHandler> logger) : base(bot)
    {
        _products = products;
        _client = client;
        _options = options.Value;
        _runtime = runtime;
        _logger = logger;
    }

    public async Task PublishAsync(BotUpdateContext ctx, Guid productId, CancellationToken ct)
    {
        var channelId = _runtime.ChannelId;
        if (string.IsNullOrWhiteSpace(channelId))
        {
            await Answer(ctx, BotMessages.AdminChannelNotConfigured, alert: true, ct: ct);
            return;
        }

        var dto = await _products.GetProductDetailAsync(productId, ct);
        if (dto is null)
        {
            await Answer(ctx, BotMessages.NothingHere, alert: true, ct: ct);
            return;
        }

        var caption = BuildChannelCaption(dto);
        var keyboard = BuildDeeplinkKeyboard(dto.Id, _options.BotUsername ?? string.Empty);
        ChatId target = long.TryParse(channelId, out var numeric) ? new ChatId(numeric) : new ChatId(channelId);

        try
        {
            if (!string.IsNullOrWhiteSpace(dto.TelegramFileId))
                await _client.SendPhoto(target, InputFile.FromFileId(dto.TelegramFileId!), caption: caption, replyMarkup: keyboard, cancellationToken: ct);
            else
                await _client.SendMessage(target, caption, replyMarkup: keyboard, cancellationToken: ct);

            await Answer(ctx, BotMessages.AdminPublished, ct: ct);
            await Send(ctx, BotMessages.AdminPublished, ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Channel publish failed for product {ProductId} to {Channel}", productId, channelId);
            await Answer(ctx, BotMessages.AdminPublishFailed, alert: true, ct: ct);
            await Send(ctx, BotMessages.AdminPublishFailed, ct: ct);
        }
    }

    private static string BuildChannelCaption(ProductDetailDto dto)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🍳 {dto.Name}");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(dto.Description))
        {
            sb.AppendLine(dto.Description);
            sb.AppendLine();
        }
        sb.AppendLine($"💰 قیمت واحد: {PriceFormatter.FormatToman(dto.Price)}");
        sb.AppendLine();
        sb.AppendLine(CatalogHandler.BuildDiscountTable(dto.DiscountTiers));
        return sb.ToString().TrimEnd();
    }

    private static InlineKeyboardMarkup BuildDeeplinkKeyboard(Guid productId, string username)
    {
        string Link(string payload) => $"https://t.me/{username}?start={payload}";
        var id = productId.ToString("N");
        return new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("🛒 سفارش دهید", Link($"product_{id}")) },
            new[]
            {
                InlineKeyboardButton.WithUrl($"📦 {PriceFormatter.FormatNumber(1)} عدد", Link($"buy_{id}_1")),
                InlineKeyboardButton.WithUrl($"📦 {PriceFormatter.FormatNumber(5)} عدد", Link($"buy_{id}_5")),
                InlineKeyboardButton.WithUrl($"📦 {PriceFormatter.FormatNumber(10)} عدد", Link($"buy_{id}_10"))
            }
        });
    }
}
