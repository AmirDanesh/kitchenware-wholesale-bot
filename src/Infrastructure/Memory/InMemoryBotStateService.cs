using System.Text.Json;
using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KitchenwareBot.Infrastructure.Memory;

/// <summary>
/// Process-local Debug implementation of bot FSM state. JSON storage mirrors Redis copy semantics,
/// while the instance gate makes session creation and checkout claims atomic within this process.
/// </summary>
public sealed class InMemoryBotStateService : IBotStateService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _ttl;
    private readonly object _gate = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public InMemoryBotStateService(IMemoryCache cache, IOptions<RedisOptions> options)
    {
        _cache = cache;
        var minutes = options.Value.SessionTtlMinutes <= 0 ? 30 : options.Value.SessionTtlMinutes;
        _ttl = TimeSpan.FromMinutes(minutes);
    }

    public Task<UserSession> GetOrCreateAsync(long telegramId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_cache.TryGetValue<string>(SessionKey(telegramId), out var json) && json is not null)
            {
                try
                {
                    var existing = JsonSerializer.Deserialize<UserSession>(json, _jsonOptions);
                    if (existing is not null)
                        return Task.FromResult(existing);
                }
                catch (JsonException)
                {
                    _cache.Remove(SessionKey(telegramId));
                }
            }

            var session = new UserSession { TelegramId = telegramId, State = BotState.Idle };
            SetSession(session);
            return Task.FromResult(session);
        }
    }

    public Task SetAsync(UserSession session, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_gate)
            SetSession(session);

        return Task.CompletedTask;
    }

    public Task ClearAsync(long telegramId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_gate)
            _cache.Remove(SessionKey(telegramId));

        return Task.CompletedTask;
    }

    public Task<bool> TryBeginCheckoutAsync(long telegramId, Guid checkoutToken, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (checkoutToken == Guid.Empty) return Task.FromResult(false);

        lock (_gate)
        {
            var key = CheckoutKey(telegramId, checkoutToken);
            if (_cache.TryGetValue(key, out _))
                return Task.FromResult(false);

            _cache.Set(key, true, CreateEntryOptions());
            return Task.FromResult(true);
        }
    }

    public Task ReleaseCheckoutAsync(long telegramId, Guid checkoutToken, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (checkoutToken == Guid.Empty) return Task.CompletedTask;

        lock (_gate)
            _cache.Remove(CheckoutKey(telegramId, checkoutToken));

        return Task.CompletedTask;
    }

    private void SetSession(UserSession session)
    {
        var json = JsonSerializer.Serialize(session, _jsonOptions);
        _cache.Set(SessionKey(session.TelegramId), json, CreateEntryOptions());
    }

    private MemoryCacheEntryOptions CreateEntryOptions()
        => new() { SlidingExpiration = _ttl };

    private static string SessionKey(long telegramId) => $"bot:session:{telegramId}";
    private static string CheckoutKey(long telegramId, Guid checkoutToken)
        => $"bot:checkout:{telegramId}:{checkoutToken:N}";
}
