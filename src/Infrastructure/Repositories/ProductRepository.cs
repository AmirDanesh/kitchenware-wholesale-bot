using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Interfaces;
using KitchenwareBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KitchenwareBot.Infrastructure.Repositories;

internal sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context) => _context = context;

    public async Task<Product?> GetByIdAsync(Guid id) =>
        await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);

    public async Task AddAsync(Product entity) => await _context.Products.AddAsync(entity);

    public Task UpdateAsync(Product entity)
    {
        _context.Products.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is not null)
        {
            product.Deactivate();
            _context.Products.Update(product);
        }
    }

    public async Task<IReadOnlyList<Product>> GetAllActiveAsync(Guid? categoryId, int page, int pageSize)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        return await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetActiveCountAsync(Guid? categoryId)
    {
        var query = _context.Products.Where(p => p.IsActive);
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);
        return await query.CountAsync();
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(string term)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && (p.Name.Contains(term) || p.Description.Contains(term)))
            .OrderBy(p => p.Name)
            .Take(50)
            .ToListAsync();
    }

    public async Task<Product?> GetWithInventoryAsync(Guid id)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.InventoryItems).ThenInclude(i => i.Warehouse)
            .Include(p => p.DiscountTiers.Where(t => t.IsActive))
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync()
    {
        return await _context.Categories
            .Where(c => c.IsActive)
            .Include(c => c.Children.Where(ch => ch.IsActive))
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
    }
}
