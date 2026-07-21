using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Repositories;

namespace KitchenwareBot.Application.Services;

public class PaymentSettingsService : IPaymentSettingsService
{
    private readonly IPaymentSettingsRepository _repo;
    private readonly IUnitOfWork _uow;

    public PaymentSettingsService(IPaymentSettingsRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public Task<PaymentSettings> GetAsync(CancellationToken ct = default) => _repo.GetAsync(ct);

    public async Task SetBankTransferEnabledAsync(bool enabled, CancellationToken ct = default)
    {
        var settings = await _repo.GetAsync(ct);
        settings.SetBankTransferEnabled(enabled);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task SetCashEnabledAsync(bool enabled, CancellationToken ct = default)
    {
        var settings = await _repo.GetAsync(ct);
        settings.SetCashEnabled(enabled);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task UpdateBankDetailsAsync(BankDetailsDto dto, CancellationToken ct = default)
    {
        var settings = await _repo.GetAsync(ct);
        settings.UpdateBankDetails(dto.BankName, dto.AccountNumber, dto.AccountName, dto.Note);
        await _uow.SaveChangesAsync(ct);
    }
}
