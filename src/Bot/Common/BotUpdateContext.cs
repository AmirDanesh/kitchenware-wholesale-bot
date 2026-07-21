using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Domain.Entities;
using Telegram.Bot.Types;

namespace KitchenwareBot.Bot.Common;

/// <summary>Everything a handler needs about the current update, assembled by the router.</summary>
public class BotUpdateContext
{
    public required Update Update { get; init; }
    public required long TelegramId { get; init; }
    public required long ChatId { get; init; }
    public required AppUser User { get; init; }
    public required UserSession Session { get; init; }
    public required bool IsAdmin { get; init; }

    public Message? Message { get; init; }
    public string? Text { get; init; }              // trimmed text of a text message
    public CallbackQuery? Callback { get; init; }
    public int? MessageId { get; init; }            // message to edit (from a callback)
    public string[] Args { get; init; } = Array.Empty<string>(); // callback data split on ':'

    public bool IsCallback => Callback is not null;
    public bool CallbackAnswered { get; set; }
    public bool PersistSession { get; set; } = true;

    public string Arg(int index) => index < Args.Length ? Args[index] : string.Empty;
}
