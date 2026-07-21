using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Domain.Common;
using KitchenwareBot.Domain.Entities;

namespace KitchenwareBot.Application.Services;

public interface IProductService
{
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct = default);
    Task<PagedResult<Product>> GetProductsAsync(Guid? categoryId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<Product>> GetAllProductsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<ProductDetailDto?> GetProductDetailAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateProductAsync(CreateProductDto dto, CancellationToken ct = default);
    Task UpdateProductAsync(Guid id, UpdateProductDto dto, CancellationToken ct = default);
    Task DeleteProductAsync(Guid id, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(Guid id, CancellationToken ct = default);
    Task SetProductImageAsync(Guid id, string? imagePath, string? telegramFileId, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> SearchProductsAsync(string term, CancellationToken ct = default);
}
