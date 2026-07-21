namespace KitchenwareBot.Bot.Configuration;

public class TelegramOptions
{
    public string BotToken { get; set; } = string.Empty;
    public string? WebhookUrl { get; set; }
    public string? WebhookSecretToken { get; set; }
    public string? BotUsername { get; set; }
    public string? ChannelId { get; set; }
    public long[] AdminIds { get; set; } = Array.Empty<long>();

    /// <summary>Webhook mode when a public URL is configured; otherwise long-polling (dev).</summary>
    public bool UseWebhook => !string.IsNullOrWhiteSpace(WebhookUrl);
}
