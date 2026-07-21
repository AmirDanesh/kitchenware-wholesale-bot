namespace KitchenwareBot.Application.Sessions;

/// <summary>Stores/loads <see cref="UserSession"/> FSM state (Redis-backed in Infrastructure).</summary>
public interface IBotStateService
{
    Task<UserSession> GetOrCreateAsync(long telegramId, CancellationToken ct = default);
    Task SetAsync(UserSession session, CancellationToken ct = default);
    Task ClearAsync(long telegramId, CancellationToken ct = default);
    Task<bool> TryBeginCheckoutAsync(long telegramId, Guid checkoutToken, CancellationToken ct = default);
    Task ReleaseCheckoutAsync(long telegramId, Guid checkoutToken, CancellationToken ct = default);
}
