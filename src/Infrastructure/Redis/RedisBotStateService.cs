using System.Text.Json;
using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace KitchenwareBot.Infrastructure.Redis;

/// <summary>
/// Stores each user's <see cref="UserSession"/> as JSON in Redis under
/// <c>bot:session:{telegramId}</c> with a sliding TTL (default 30 minutes).
/// </summary>
public class RedisBotStateService : IBotStateService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _ttl;

    public RedisBotStateService(IConnectionMultiplexer redis, IOptions<RedisOptions> options)
    {
        _redis = redis;
        var minutes = options.Value.SessionTtlMinutes <= 0 ? 30 : options.Value.SessionTtlMinutes;
        _ttl = TimeSpan.FromMinutes(minutes);
    }

    private static string Key(long telegramId) => $"bot:session:{telegramId}";

    public async Task<UserSession> GetOrCreateAsync(long telegramId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = Key(telegramId);
        var value = await db.StringGetAsync(key);

        if (value.HasValue)
        {
            try
            {
                var existing = JsonSerializer.Deserialize<UserSession>(value!, JsonOptions);
                if (existing is not null)
                {
                    await db.KeyExpireAsync(key, _ttl); // sliding expiration
                    return existing;
                }
            }
            catch (JsonException)
            {
                // Corrupt payload — fall through and recreate a fresh session.
            }
        }

        var session = new UserSession { TelegramId = telegramId, State = BotState.Idle };
        await SetAsync(session, ct);
        return session;
    }

    public async Task SetAsync(UserSession session, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(session, JsonOptions);
        await db.StringSetAsync(Key(session.TelegramId), json, _ttl);
    }

    public async Task ClearAsync(long telegramId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(Key(telegramId));
    }
}
