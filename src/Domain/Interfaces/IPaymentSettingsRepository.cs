using KitchenwareBot.Domain.Entities;

namespace KitchenwareBot.Domain.Interfaces;

public interface IPaymentSettingsRepository
{
    Task<PaymentSettings> GetAsync();
    Task SaveAsync(PaymentSettings settings);
}
