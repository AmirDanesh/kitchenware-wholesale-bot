using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KitchenwareBot.Infrastructure.Persistence.Repositories;

public class DiscountRepository : IDiscountRepository
{
    private readonly AppDbContext _db;
    public DiscountRepository(AppDbContext db) => _db = db;

    // ── Global tiers ─────────────────────────────────────────────
    public async Task<IReadOnlyList<GlobalDiscountTier>> GetGlobalTiersAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var query = _db.GlobalDiscountTiers.AsQueryable();
        if (activeOnly) query = query.Where(t => t.IsActive);
        return await query.OrderBy(t => t.MinQuantity).ThenBy(t => t.DisplayOrder).ToListAsync(ct);
    }

    public Task<GlobalDiscountTier?> GetGlobalTierByIdAsync(Guid id, CancellationToken ct = default)
        => _db.GlobalDiscountTiers.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddGlobalTierAsync(GlobalDiscountTier tier, CancellationToken ct = default)
        => await _db.GlobalDiscountTiers.AddAsync(tier, ct);

    public void UpdateGlobalTier(GlobalDiscountTier tier) => _db.GlobalDiscountTiers.Update(tier);
    public void RemoveGlobalTier(GlobalDiscountTier tier) => _db.GlobalDiscountTiers.Remove(tier);

    // ── Product tiers ────────────────────────────────────────────
    public async Task<IReadOnlyList<ProductDiscountTier>> GetProductTiersAsync(Guid productId, bool activeOnly = true, CancellationToken ct = default)
    {
        var query = _db.ProductDiscountTiers.Where(t => t.ProductId == productId);
        if (activeOnly) query = query.Where(t => t.IsActive);
        return await query.OrderBy(t => t.MinQuantity).ThenBy(t => t.DisplayOrder).ToListAsync(ct);
    }

    public Task<ProductDiscountTier?> GetProductTierByIdAsync(Guid id, CancellationToken ct = default)
        => _db.ProductDiscountTiers.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddProductTierAsync(ProductDiscountTier tier, CancellationToken ct = default)
        => await _db.ProductDiscountTiers.AddAsync(tier, ct);

    public void UpdateProductTier(ProductDiscountTier tier) => _db.ProductDiscountTiers.Update(tier);
    public void RemoveProductTier(ProductDiscountTier tier) => _db.ProductDiscountTiers.Remove(tier);

    public async Task RemoveAllProductTiersAsync(Guid productId, CancellationToken ct = default)
    {
        var tiers = await _db.ProductDiscountTiers.Where(t => t.ProductId == productId).ToListAsync(ct);
        if (tiers.Count > 0)
            _db.ProductDiscountTiers.RemoveRange(tiers);
    }

    // ── Resolution ───────────────────────────────────────────────
    public async Task<decimal> ResolveDiscountAsync(Guid productId, int quantity, CancellationToken ct = default)
    {
        // 1-2) Product tiers, when present, replace global entirely.
        var productTiers = await _db.ProductDiscountTiers.AsNoTracking()
            .Where(t => t.ProductId == productId && t.IsActive)
            .OrderBy(t => t.MinQuantity)
            .ToListAsync(ct);

        if (productTiers.Count > 0)
            return MatchPercent(productTiers.Select(t => (t.MinQuantity, t.MaxQuantity, t.DiscountPercent)), quantity);

        // 3) Otherwise fall back to global tiers.
        var globalTiers = await _db.GlobalDiscountTiers.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.MinQuantity)
            .ToListAsync(ct);

        return MatchPercent(globalTiers.Select(t => (t.MinQuantity, t.MaxQuantity, t.DiscountPercent)), quantity);
    }

    private static decimal MatchPercent(IEnumerable<(int Min, int? Max, decimal Percent)> tiers, int quantity)
    {
        foreach (var t in tiers)
            if (quantity >= t.Min && (t.Max is null || quantity <= t.Max))
                return t.Percent;
        return 0m;
    }
}
