using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Domain.Entities;

namespace KitchenwareBot.Application.Services;

public interface IPaymentSettingsService
{
    Task<PaymentSettings> GetAsync(CancellationToken ct = default);
    Task SetBankTransferEnabledAsync(bool enabled, CancellationToken ct = default);
    Task SetCashEnabledAsync(bool enabled, CancellationToken ct = default);
    Task UpdateBankDetailsAsync(BankDetailsDto dto, CancellationToken ct = default);
}
