using FluentValidation;
using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Domain.Common;
using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Exceptions;
using KitchenwareBot.Domain.Repositories;

namespace KitchenwareBot.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _products;
    private readonly IDiscountRepository _discounts;
    private readonly IInventoryRepository _inventory;
    private readonly IWarehouseRepository _warehouses;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CreateProductDto> _createValidator;

    public ProductService(
        IProductRepository products,
        IDiscountRepository discounts,
        IInventoryRepository inventory,
        IWarehouseRepository warehouses,
        IUnitOfWork uow,
        IValidator<CreateProductDto> createValidator)
    {
        _products = products;
        _discounts = discounts;
        _inventory = inventory;
        _warehouses = warehouses;
        _uow = uow;
        _createValidator = createValidator;
    }

    public Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct = default)
        => _products.GetCategoriesAsync(activeOnly: true, ct);

    public Task<PagedResult<Product>> GetProductsAsync(Guid? categoryId, int page, int pageSize, CancellationToken ct = default)
        => _products.GetAllActiveAsync(categoryId, page, pageSize, ct);

    public Task<PagedResult<Product>> GetAllProductsAsync(int page, int pageSize, CancellationToken ct = default)
        => _products.GetAllAsync(page, pageSize, ct);

    public async Task<ProductDetailDto?> GetProductDetailAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _products.GetWithInventoryAsync(id, ct);
        if (product is null) return null;

        var available = product.InventoryItems.Sum(i => i.Quantity - i.ReservedQuantity);
        var maxThreshold = product.InventoryItems.Select(i => i.LowStockThreshold).DefaultIfEmpty(5).Max();

        var productTiers = product.DiscountTiers.Where(t => t.IsActive).OrderBy(t => t.MinQuantity).ToList();
        bool usesProductTiers = productTiers.Count > 0;

        IReadOnlyList<DiscountTierDto> tiers;
        if (usesProductTiers)
        {
            tiers = productTiers
                .Select(t => new DiscountTierDto(t.MinQuantity, t.MaxQuantity, t.DiscountPercent, t.Id))
                .ToList();
        }
        else
        {
            var globals = await _discounts.GetGlobalTiersAsync(activeOnly: true, ct);
            tiers = globals
                .Select(t => new DiscountTierDto(t.MinQuantity, t.MaxQuantity, t.DiscountPercent, t.Id))
                .ToList();
        }

        return new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            ImagePath = product.ImagePath,
            TelegramFileId = product.TelegramFileId,
            IsActive = product.IsActive,
            AvailableStock = available,
            IsLowStock = available <= maxThreshold,
            UsesProductTiers = usesProductTiers,
            DiscountTiers = tiers
        };
    }

    public async Task<Guid> CreateProductAsync(CreateProductDto dto, CancellationToken ct = default)
    {
        await _createValidator.ValidateAndThrowAsync(dto, ct);

        var product = Product.Create(dto.Name, dto.Description, dto.Price, dto.CategoryId);
        if (dto.ImagePath is not null || dto.TelegramFileId is not null)
            product.SetImage(dto.ImagePath, dto.TelegramFileId);
        await _products.AddAsync(product, ct);

        var warehouse = await _warehouses.GetDefaultAsync(ct)
                        ?? throw new EntityNotFoundException(nameof(Warehouse), "default");
        var inventory = InventoryItem.Create(product.Id, warehouse.Id, Math.Max(0, dto.InitialStock));
        await _inventory.AddAsync(inventory, ct);

        await _uow.SaveChangesAsync(ct);
        return product.Id;
    }

    public async Task UpdateProductAsync(Guid id, UpdateProductDto dto, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(id, ct)
                      ?? throw new EntityNotFoundException(nameof(Product), id);
        product.Update(dto.Name, dto.Description, dto.Price, dto.CategoryId);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(id, ct);
        if (product is null) return;
        product.Deactivate(); // soft delete
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<bool> ToggleActiveAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(id, ct)
                      ?? throw new EntityNotFoundException(nameof(Product), id);
        if (product.IsActive) product.Deactivate();
        else product.Activate();
        await _uow.SaveChangesAsync(ct);
        return product.IsActive;
    }

    public async Task SetProductImageAsync(Guid id, string? imagePath, string? telegramFileId, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(id, ct)
                      ?? throw new EntityNotFoundException(nameof(Product), id);
        product.SetImage(imagePath, telegramFileId);
        await _uow.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<Product>> SearchProductsAsync(string term, CancellationToken ct = default)
        => _products.SearchAsync(term, ct);
}
