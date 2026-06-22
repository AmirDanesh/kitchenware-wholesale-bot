using KitchenwareBot.Domain.Entities;

namespace KitchenwareBot.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<IReadOnlyList<Product>> GetAllActiveAsync(Guid? categoryId, int page, int pageSize);
    Task<int> GetActiveCountAsync(Guid? categoryId);
    Task<IReadOnlyList<Product>> SearchAsync(string term);
    Task<Product?> GetWithInventoryAsync(Guid id);
    Task<IReadOnlyList<Category>> GetCategoriesAsync();
}
