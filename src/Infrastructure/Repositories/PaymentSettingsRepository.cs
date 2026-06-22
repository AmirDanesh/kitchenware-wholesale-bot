using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Interfaces;
using KitchenwareBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KitchenwareBot.Infrastructure.Repositories;

internal sealed class PaymentSettingsRepository : IPaymentSettingsRepository
{
    private readonly AppDbContext _context;

    public PaymentSettingsRepository(AppDbContext context) => _context = context;

    public async Task<PaymentSettings> GetAsync() =>
        await _context.PaymentSettings.SingleOrDefaultAsync()
        ?? PaymentSettings.CreateDefault();

    public Task SaveAsync(PaymentSettings settings)
    {
        _context.PaymentSettings.Update(settings);
        return Task.CompletedTask;
    }
}
