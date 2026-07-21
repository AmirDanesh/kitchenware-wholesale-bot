using FluentValidation;
using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Exceptions;
using KitchenwareBot.Domain.Repositories;

namespace KitchenwareBot.Application.Services;

public class DiscountService : IDiscountService
{
    private readonly IDiscountRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<TierInputDto> _validator;

    public DiscountService(IDiscountRepository repo, IUnitOfWork uow, IValidator<TierInputDto> validator)
    {
        _repo = repo;
        _uow = uow;
        _validator = validator;
    }

    // ── Global tiers (admin sees all, including inactive) ─────────
    public Task<IReadOnlyList<GlobalDiscountTier>> GetGlobalTiersAsync(CancellationToken ct = default)
        => _repo.GetGlobalTiersAsync(activeOnly: false, ct);

    public async Task<Guid> AddGlobalTierAsync(TierInputDto dto, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(dto, ct);
        var existing = await _repo.GetGlobalTiersAsync(false, ct);
        var tier = GlobalDiscountTier.Create(dto.MinQuantity, dto.MaxQuantity, dto.DiscountPercent, existing.Count);
        await _repo.AddGlobalTierAsync(tier, ct);
        await _uow.SaveChangesAsync(ct);
        return tier.Id;
    }

    public async Task UpdateGlobalTierAsync(Guid id, TierInputDto dto, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(dto, ct);
        var tier = await _repo.GetGlobalTierByIdAsync(id, ct)
                   ?? throw new EntityNotFoundException(nameof(GlobalDiscountTier), id);
        tier.Update(dto.MinQuantity, dto.MaxQuantity, dto.DiscountPercent);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task DeleteGlobalTierAsync(Guid id, CancellationToken ct = default)
    {
        var tier = await _repo.GetGlobalTierByIdAsync(id, ct);
        if (tier is null) return;
        _repo.RemoveGlobalTier(tier);
        await _uow.SaveChangesAsync(ct);
    }

    // ── Product tiers ─────────────────────────────────────────────
    public Task<IReadOnlyList<ProductDiscountTier>> GetProductTiersAsync(Guid productId, CancellationToken ct = default)
        => _repo.GetProductTiersAsync(productId, activeOnly: false, ct);

    public async Task<Guid> AddProductTierAsync(Guid productId, TierInputDto dto, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(dto, ct);
        var existing = await _repo.GetProductTiersAsync(productId, false, ct);
        var tier = ProductDiscountTier.Create(productId, dto.MinQuantity, dto.MaxQuantity, dto.DiscountPercent, existing.Count);
        await _repo.AddProductTierAsync(tier, ct);
        await _uow.SaveChangesAsync(ct);
        return tier.Id;
    }

    public async Task UpdateProductTierAsync(Guid id, TierInputDto dto, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(dto, ct);
        var tier = await _repo.GetProductTierByIdAsync(id, ct)
                   ?? throw new EntityNotFoundException(nameof(ProductDiscountTier), id);
        tier.Update(dto.MinQuantity, dto.MaxQuantity, dto.DiscountPercent);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task DeleteProductTierAsync(Guid id, CancellationToken ct = default)
    {
        var tier = await _repo.GetProductTierByIdAsync(id, ct);
        if (tier is null) return;
        _repo.RemoveProductTier(tier);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task RemoveProductTiersAsync(Guid productId, CancellationToken ct = default)
    {
        await _repo.RemoveAllProductTiersAsync(productId, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public Task<decimal> ResolveDiscountAsync(Guid productId, int quantity, CancellationToken ct = default)
        => _repo.ResolveDiscountAsync(productId, quantity, ct);
}
