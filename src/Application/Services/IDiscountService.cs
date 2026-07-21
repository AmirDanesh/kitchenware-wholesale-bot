using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Domain.Entities;

namespace KitchenwareBot.Application.Services;

public interface IDiscountService
{
    // Global tiers
    Task<IReadOnlyList<GlobalDiscountTier>> GetGlobalTiersAsync(CancellationToken ct = default);
    Task<Guid> AddGlobalTierAsync(TierInputDto dto, CancellationToken ct = default);
    Task UpdateGlobalTierAsync(Guid id, TierInputDto dto, CancellationToken ct = default);
    Task DeleteGlobalTierAsync(Guid id, CancellationToken ct = default);

    // Product tiers
    Task<IReadOnlyList<ProductDiscountTier>> GetProductTiersAsync(Guid productId, CancellationToken ct = default);
    Task<Guid> AddProductTierAsync(Guid productId, TierInputDto dto, CancellationToken ct = default);
    Task UpdateProductTierAsync(Guid id, TierInputDto dto, CancellationToken ct = default);
    Task DeleteProductTierAsync(Guid id, CancellationToken ct = default);
    Task RemoveProductTiersAsync(Guid productId, CancellationToken ct = default);

    Task<decimal> ResolveDiscountAsync(Guid productId, int quantity, CancellationToken ct = default);
}
