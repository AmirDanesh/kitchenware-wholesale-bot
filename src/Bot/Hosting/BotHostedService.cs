using KitchenwareBot.Bot.Configuration;
using KitchenwareBot.Bot.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KitchenwareBot.Bot.Hosting;

/// <summary>
/// Bootstraps update delivery. In webhook mode it registers the webhook and returns
/// (updates arrive over HTTP). In dev (no WebhookUrl) it long-polls getUpdates.
/// </summary>
public class BotHostedService : BackgroundService
{
    private readonly ITelegramBotClient _bot;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TelegramOptions _options;
    private readonly ILogger<BotHostedService> _logger;

    public BotHostedService(ITelegramBotClient bot, IServiceScopeFactory scopeFactory,
        IOptions<TelegramOptions> options, ILogger<BotHostedService> logger)
    {
        _bot = bot;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            _logger.LogWarning("Telegram:BotToken is not configured — the bot will not receive updates.");
            return;
        }

        if (_options.UseWebhook)
        {
            await SetupWebhookAsync(stoppingToken);
            return; // updates arrive on /telegram/webhook
        }

        _logger.LogInformation("Starting Telegram long-polling (development mode).");
        try { await _bot.DeleteWebhook(dropPendingUpdates: true, cancellationToken: stoppingToken); }
        catch (Exception ex) { _logger.LogWarning(ex, "DeleteWebhook failed"); }

        int? offset = null;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updates = await _bot.GetUpdates(offset: offset, timeout: 30, cancellationToken: stoppingToken);
                foreach (var update in updates)
                {
                    await ProcessAsync(update, stoppingToken);
                    offset = update.Id + 1;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Polling error; retrying in a few seconds.");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    public async Task ProcessAsync(Update update, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var router = scope.ServiceProvider.GetRequiredService<UpdateRouter>();
        await router.RouteAsync(update, ct);
    }

    private async Task SetupWebhookAsync(CancellationToken ct)
    {
        try
        {
            var secret = string.IsNullOrWhiteSpace(_options.WebhookSecretToken) ? null : _options.WebhookSecretToken;
            await _bot.SetWebhook(_options.WebhookUrl!, secretToken: secret, dropPendingUpdates: true, cancellationToken: ct);
            _logger.LogInformation("Telegram webhook registered at {Url}", _options.WebhookUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register Telegram webhook");
        }
    }
}
