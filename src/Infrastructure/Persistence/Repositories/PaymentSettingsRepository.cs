using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KitchenwareBot.Infrastructure.Persistence.Repositories;

public class PaymentSettingsRepository : IPaymentSettingsRepository
{
    private readonly AppDbContext _db;
    public PaymentSettingsRepository(AppDbContext db) => _db = db;

    public async Task<PaymentSettings> GetAsync(CancellationToken ct = default)
    {
        var settings = await _db.PaymentSettings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            // Failsafe if the seed row is somehow missing.
            settings = PaymentSettings.CreateDefault();
            await _db.PaymentSettings.AddAsync(settings, ct);
        }
        return settings;
    }

    public void Update(PaymentSettings settings) => _db.PaymentSettings.Update(settings);
}
