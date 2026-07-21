namespace KitchenwareBot.Application.DTOs;

/// <summary>Input for creating/updating a discount tier (global or product-specific).</summary>
public record TierInputDto(int MinQuantity, int? MaxQuantity, decimal DiscountPercent);
