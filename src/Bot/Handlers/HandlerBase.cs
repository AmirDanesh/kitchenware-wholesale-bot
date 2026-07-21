using KitchenwareBot.Bot.Common;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace KitchenwareBot.Bot.Handlers;

/// <summary>Shared UX helpers for handlers: show a screen (edit on callback, else send),
/// send a fresh message, and answer callback toasts.</summary>
public abstract class HandlerBase
{
    protected readonly BotResponder Bot;
    protected HandlerBase(BotResponder bot) => Bot = bot;

    /// <summary>Renders an inline "screen": edits the current message when triggered by a
    /// callback, otherwise sends a new message.</summary>
    protected Task Show(BotUpdateContext ctx, string text, InlineKeyboardMarkup? markup = null, CancellationToken ct = default)
        => ctx.IsCallback && ctx.MessageId is int messageId
            ? Bot.EditAsync(ctx.ChatId, messageId, text, markup, ct)
            : Bot.SendAsync(ctx.ChatId, text, markup, ct);

    protected Task<Message> Send(BotUpdateContext ctx, string text, ReplyMarkup? markup = null, CancellationToken ct = default)
        => Bot.SendAsync(ctx.ChatId, text, markup, ct);

    protected Task SendPhoto(BotUpdateContext ctx, string fileId, string caption, InlineKeyboardMarkup? markup = null, CancellationToken ct = default)
        => Bot.SendPhotoAsync(ctx.ChatId, fileId, caption, markup, ct);

    protected async Task Answer(BotUpdateContext ctx, string? text = null, bool alert = false, CancellationToken ct = default)
    {
        if (ctx.Callback is null) return;
        await Bot.AnswerAsync(ctx.Callback.Id, text, alert, ct);
        ctx.CallbackAnswered = true;
    }
}
