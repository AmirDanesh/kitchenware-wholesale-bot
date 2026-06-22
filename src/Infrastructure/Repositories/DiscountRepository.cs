using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Interfaces;
using KitchenwareBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KitchenwareBot.Infrastructure.Repositories;

internal sealed class DiscountRepository : IDiscountRepository
{
    private readonly AppDbContext _context;

    public DiscountRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<GlobalDiscountTier>> GetGlobalTiersAsync() =>
        await _context.GlobalDiscountTiers
            .Where(t => t.IsActive)
            .OrderBy(t => t.MinQuantity)
            .ToListAsync();

    public async Task AddGlobalTierAsync(GlobalDiscountTier tier) =>
        await _context.GlobalDiscountTiers.AddAsync(tier);

    public Task UpdateGlobalTierAsync(GlobalDiscountTier tier)
    {
        _context.GlobalDiscountTiers.Update(tier);
        return Task.CompletedTask;
    }

    public async Task DeleteGlobalTierAsync(Guid id)
    {
        var tier = await _context.GlobalDiscountTiers.FindAsync(id);
        if (tier is not null) _context.GlobalDiscountTiers.Remove(tier);
    }

    public async Task<IReadOnlyList<ProductDiscountTier>> GetProductTiersAsync(Guid productId) =>
        await _context.ProductDiscountTiers
            .Where(t => t.ProductId == productId && t.IsActive)
            .OrderBy(t => t.MinQuantity)
            .ToListAsync();

    public async Task AddProductTierAsync(ProductDiscountTier tier) =>
        await _context.ProductDiscountTiers.AddAsync(tier);

    public Task UpdateProductTierAsync(ProductDiscountTier tier)
    {
        _context.ProductDiscountTiers.Update(tier);
        return Task.CompletedTask;
    }

    public async Task DeleteProductTierAsync(Guid id)
    {
        var tier = await _context.ProductDiscountTiers.FindAsync(id);
        if (tier is not null) _context.ProductDiscountTiers.Remove(tier);
    }
}
