using System.Text.Json;
using KitchenwareBot.Application.Interfaces;
using KitchenwareBot.Application.Models;
using StackExchange.Redis;

namespace KitchenwareBot.Infrastructure.Services;

internal sealed class RedisBotStateService : IBotStateService
{
    private readonly IDatabase _db;
    private const string KeyPrefix = "bot:session:";
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(30);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RedisBotStateService(IConnectionMultiplexer redis) => _db = redis.GetDatabase();

    public async Task<UserSession> GetOrCreateAsync(long telegramId)
    {
        var json = await _db.StringGetAsync($"{KeyPrefix}{telegramId}");
        if (!json.IsNullOrEmpty)
        {
            var existing = JsonSerializer.Deserialize<UserSession>((string)json!, JsonOptions);
            if (existing is not null) return existing;
        }

        var session = new UserSession { TelegramId = telegramId };
        await SetAsync(telegramId, session);
        return session;
    }

    public async Task SetAsync(long telegramId, UserSession session)
    {
        var json = JsonSerializer.Serialize(session, JsonOptions);
        await _db.StringSetAsync($"{KeyPrefix}{telegramId}", json, SessionTtl);
    }

    public async Task ClearAsync(long telegramId) =>
        await _db.KeyDeleteAsync($"{KeyPrefix}{telegramId}");
}
