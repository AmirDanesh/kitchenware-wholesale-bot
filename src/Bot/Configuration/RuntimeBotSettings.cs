namespace KitchenwareBot.Bot.Configuration;

/// <summary>
/// Mutable runtime settings a singleton holds for the app's lifetime. The channel id starts from
/// configuration (Telegram:ChannelId) and can be updated by an admin at runtime. For a permanent
/// change, set the Telegram__ChannelId environment variable (survives restarts).
/// </summary>
public class RuntimeBotSettings
{
    public string? ChannelId { get; set; }

    public RuntimeBotSettings(string? initialChannelId) => ChannelId = initialChannelId;
}
