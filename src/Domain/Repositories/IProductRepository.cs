using KitchenwareBot.Domain.Common;
using KitchenwareBot.Domain.Entities;

namespace KitchenwareBot.Domain.Repositories;

public interface IProductRepository
{
    // Products
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Product?> GetWithInventoryAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<Product>> GetAllActiveAsync(Guid? categoryId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<Product>> GetAllAsync(int page, int pageSize, CancellationToken ct = default); // admin: includes inactive
    Task<IReadOnlyList<Product>> SearchAsync(string term, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    void Update(Product product);

    // Categories (read + seed/admin). Categories nest one level (parent → child).
    Task<IReadOnlyList<Category>> GetCategoriesAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<Category?> GetCategoryByIdAsync(Guid id, CancellationToken ct = default);
    Task AddCategoryAsync(Category category, CancellationToken ct = default);
}
