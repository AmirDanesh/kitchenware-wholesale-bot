using KitchenwareBot.Domain.Common;
using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KitchenwareBot.Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;
    public ProductRepository(AppDbContext db) => _db = db;

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Product?> GetWithInventoryAsync(Guid id, CancellationToken ct = default)
        => _db.Products
            .Include(p => p.Category)
            .Include(p => p.InventoryItems)
            .Include(p => p.DiscountTiers)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<PagedResult<Product>> GetAllActiveAsync(Guid? categoryId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Products.AsNoTracking().Where(p => p.IsActive);
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);
        return query.OrderBy(p => p.Name).ToPagedResultAsync(page, pageSize, ct);
    }

    public Task<PagedResult<Product>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
        => _db.Products.AsNoTracking().OrderByDescending(p => p.CreatedAt).ToPagedResultAsync(page, pageSize, ct);

    public async Task<IReadOnlyList<Product>> SearchAsync(string term, CancellationToken ct = default)
    {
        term = (term ?? string.Empty).Trim();
        var pattern = $"%{term}%";
        return await _db.Products.AsNoTracking()
            .Where(p => p.IsActive && (EF.Functions.Like(p.Name, pattern) || EF.Functions.Like(p.Description, pattern)))
            .OrderBy(p => p.Name)
            .Take(50)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Product product, CancellationToken ct = default)
        => await _db.Products.AddAsync(product, ct);

    public void Update(Product product) => _db.Products.Update(product);

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var query = _db.Categories.AsNoTracking().AsQueryable();
        if (activeOnly) query = query.Where(c => c.IsActive);
        return await query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync(ct);
    }

    public Task<Category?> GetCategoryByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddCategoryAsync(Category category, CancellationToken ct = default)
        => await _db.Categories.AddAsync(category, ct);
}
