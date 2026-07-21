using KitchenwareBot.Domain.Entities;

namespace KitchenwareBot.Domain.Repositories;

public interface IDiscountRepository
{
    // Global tiers (ordered by MinQuantity)
    Task<IReadOnlyList<GlobalDiscountTier>> GetGlobalTiersAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<GlobalDiscountTier?> GetGlobalTierByIdAsync(Guid id, CancellationToken ct = default);
    Task AddGlobalTierAsync(GlobalDiscountTier tier, CancellationToken ct = default);
    void UpdateGlobalTier(GlobalDiscountTier tier);
    void RemoveGlobalTier(GlobalDiscountTier tier);

    // Product-specific tiers
    Task<IReadOnlyList<ProductDiscountTier>> GetProductTiersAsync(Guid productId, bool activeOnly = true, CancellationToken ct = default);
    Task<ProductDiscountTier?> GetProductTierByIdAsync(Guid id, CancellationToken ct = default);
    Task AddProductTierAsync(ProductDiscountTier tier, CancellationToken ct = default);
    void UpdateProductTier(ProductDiscountTier tier);
    void RemoveProductTier(ProductDiscountTier tier);
    Task RemoveAllProductTiersAsync(Guid productId, CancellationToken ct = default);

    /// <summary>
    /// Resolves the discount percent for a product at a given quantity.
    /// If the product has active tiers, only those are considered; otherwise the global tiers
    /// are used. Returns 0 when no tier matches.
    /// </summary>
    Task<decimal> ResolveDiscountAsync(Guid productId, int quantity, CancellationToken ct = default);
}
