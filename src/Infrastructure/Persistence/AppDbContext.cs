using KitchenwareBot.Application.Interfaces;
using KitchenwareBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitchenwareBot.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<GlobalDiscountTier> GlobalDiscountTiers => Set<GlobalDiscountTier>();
    public DbSet<ProductDiscountTier> ProductDiscountTiers => Set<ProductDiscountTier>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<PaymentSettings> PaymentSettings => Set<PaymentSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
