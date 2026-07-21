using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace KitchenwareBot.Bot.Common;

/// <summary>Thin wrapper over the Telegram client with resilient edit/answer helpers.
/// Messages are plain text (no parse mode) so Persian + user-generated content is always safe.</summary>
public class BotResponder
{
    private readonly ITelegramBotClient _bot;
    public BotResponder(ITelegramBotClient bot) => _bot = bot;

    public Task<Message> SendAsync(long chatId, string text, ReplyMarkup? markup = null, CancellationToken ct = default)
        => WithRetryAsync(() => _bot.SendMessage(chatId, text, replyMarkup: markup, cancellationToken: ct), ct);

    public Task<Message> SendPhotoAsync(long chatId, string fileId, string caption, ReplyMarkup? markup = null, CancellationToken ct = default)
        => WithRetryAsync(() => _bot.SendPhoto(chatId, InputFile.FromFileId(fileId), caption: caption, replyMarkup: markup, cancellationToken: ct), ct);

    /// <summary>Retries on Telegram rate limiting (HTTP 429) with a short backoff.</summary>
    private static async Task<T> WithRetryAsync<T>(Func<Task<T>> action, CancellationToken ct, int maxAttempts = 3)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await action();
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == 429 && attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(5, attempt * 2)), ct);
            }
        }
    }

    /// <summary>Edits an existing message; silently ignores "message is not modified"/too-old errors.</summary>
    public async Task EditAsync(long chatId, int messageId, string text, InlineKeyboardMarkup? markup = null, CancellationToken ct = default)
    {
        try
        {
            await _bot.EditMessageText(chatId, messageId, text, replyMarkup: markup, cancellationToken: ct);
        }
        catch (ApiRequestException)
        {
            // e.g. "message is not modified" or the original had a photo — fall back to a new message.
            try { await _bot.SendMessage(chatId, text, replyMarkup: markup, cancellationToken: ct); }
            catch (ApiRequestException) { /* give up quietly */ }
        }
    }

    public async Task AnswerAsync(string callbackQueryId, string? text = null, bool alert = false, CancellationToken ct = default)
    {
        try
        {
            await _bot.AnswerCallbackQuery(callbackQueryId, text: text, showAlert: alert, cancellationToken: ct);
        }
        catch (ApiRequestException)
        {
            // Callback query can expire; ignore.
        }
    }
}
