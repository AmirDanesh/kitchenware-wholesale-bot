using KitchenwareBot.Domain.Entities;

namespace KitchenwareBot.Domain.Interfaces;

public interface IDiscountRepository
{
    // Global tiers
    Task<IReadOnlyList<GlobalDiscountTier>> GetGlobalTiersAsync();
    Task AddGlobalTierAsync(GlobalDiscountTier tier);
    Task UpdateGlobalTierAsync(GlobalDiscountTier tier);
    Task DeleteGlobalTierAsync(Guid id);

    // Product-specific tiers
    Task<IReadOnlyList<ProductDiscountTier>> GetProductTiersAsync(Guid productId);
    Task AddProductTierAsync(ProductDiscountTier tier);
    Task UpdateProductTierAsync(ProductDiscountTier tier);
    Task DeleteProductTierAsync(Guid id);

}
