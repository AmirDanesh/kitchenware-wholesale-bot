using System.Text.Json;
using KitchenwareBot.Application;
using KitchenwareBot.Application.Abstractions;
using KitchenwareBot.Bot.Common;
using KitchenwareBot.Bot.Configuration;
using KitchenwareBot.Bot.Handlers;
using KitchenwareBot.Bot.Hosting;
using KitchenwareBot.Bot.Notifications;
using KitchenwareBot.Bot.Routing;
using KitchenwareBot.Infrastructure;
using KitchenwareBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

var builder = WebApplication.CreateBuilder(args);

// ── Options ───────────────────────────────────────────────────────
builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection("Telegram"));

// ── Application + Infrastructure ──────────────────────────────────
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// ── Telegram client + bot plumbing ────────────────────────────────
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
    return new TelegramBotClient(options.BotToken ?? string.Empty);
});
builder.Services.AddSingleton(_ => new RuntimeBotSettings(builder.Configuration["Telegram:ChannelId"]));
builder.Services.AddSingleton<BotResponder>();
builder.Services.AddSingleton<INotificationService, TelegramNotificationService>();

// Handlers (scoped) + router
builder.Services.AddScoped<StartHandler>();
builder.Services.AddScoped<CatalogHandler>();
builder.Services.AddScoped<CartHandler>();
builder.Services.AddScoped<CheckoutHandler>();
builder.Services.AddScoped<MyOrdersHandler>();
builder.Services.AddScoped<AdminMenuHandler>();
builder.Services.AddScoped<ProductAdminHandler>();
builder.Services.AddScoped<ChannelPublishHandler>();
builder.Services.AddScoped<OrderAdminHandler>();
builder.Services.AddScoped<InventoryAdminHandler>();
builder.Services.AddScoped<DiscountAdminHandler>();
builder.Services.AddScoped<SettingsAdminHandler>();
builder.Services.AddScoped<UpdateRouter>();

builder.Services.AddHostedService<BotHostedService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

// ── Initialize persistence on startup (idempotent) ────────────────
await InitializeDatabaseAsync(app);

// ── Endpoints ─────────────────────────────────────────────────────
app.MapHealthChecks("/health");
app.MapGet("/", () => "KitchenwareBot is running.");

// Telegram webhook (production). Validates the secret-token header when configured.
app.MapPost("/telegram/webhook", async (HttpContext http, UpdateRouter router, IOptions<TelegramOptions> opt, CancellationToken ct) =>
{
    var options = opt.Value;
    if (!string.IsNullOrWhiteSpace(options.WebhookSecretToken))
    {
        var header = http.Request.Headers["X-Telegram-Bot-Api-Secret-Token"].ToString();
        if (!string.Equals(header, options.WebhookSecretToken, StringComparison.Ordinal))
            return Results.Unauthorized();
    }

    using var reader = new StreamReader(http.Request.Body);
    var body = await reader.ReadToEndAsync(ct);
    try
    {
        var update = JsonSerializer.Deserialize<Update>(body, JsonBotAPI.Options);
        if (update is not null)
            await router.RouteAsync(update, ct);
    }
    catch (Exception)
    {
        // Never fail a webhook delivery — Telegram would retry indefinitely.
    }
    return Results.Ok();
});

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (string.Equals(db.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory",
                StringComparison.Ordinal))
        {
            await db.Database.EnsureCreatedAsync();
            logger.LogInformation("Debug in-memory database initialized.");
        }
        else
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize database on startup.");
    }
}

// Exposed for integration testing.
public partial class Program { }
