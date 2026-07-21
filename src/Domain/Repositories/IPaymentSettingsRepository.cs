using KitchenwareBot.Domain.Entities;

namespace KitchenwareBot.Domain.Repositories;

public interface IPaymentSettingsRepository
{
    /// <summary>Returns the singleton settings row, creating a default (both methods off) if absent.</summary>
    Task<PaymentSettings> GetAsync(CancellationToken ct = default);
    void Update(PaymentSettings settings);
}
