namespace KitchenwareBot.Application.DTOs;

public record CreateProductDto(
    string Name,
    string? Description,
    decimal Price,
    Guid CategoryId,
    int InitialStock,
    string? TelegramFileId = null,
    string? ImagePath = null);

public record UpdateProductDto(
    string Name,
    string? Description,
    decimal Price,
    Guid CategoryId);

/// <summary>A single discount tier row shown to customers/admins.</summary>
public record DiscountTierDto(int MinQuantity, int? MaxQuantity, decimal DiscountPercent, Guid? Id = null);

/// <summary>Rich product view for the detail screen: stock indicator + resolved discount table.</summary>
public record ProductDetailDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required decimal Price { get; init; }
    public required Guid CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public string? ImagePath { get; init; }
    public string? TelegramFileId { get; init; }
    public required bool IsActive { get; init; }
    public required int AvailableStock { get; init; }
    public required bool IsLowStock { get; init; }
    /// <summary>True when the product defines its own tiers (global tiers are ignored for it).</summary>
    public required bool UsesProductTiers { get; init; }
    public required IReadOnlyList<DiscountTierDto> DiscountTiers { get; init; }
}
