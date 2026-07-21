using KitchenwareBot.Domain.Common;
using KitchenwareBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitchenwareBot.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<GlobalDiscountTier> GlobalDiscountTiers => Set<GlobalDiscountTier>();
    public DbSet<ProductDiscountTier> ProductDiscountTiers => Set<ProductDiscountTier>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<PaymentSettings> PaymentSettings => Set<PaymentSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // All monetary/percent decimals use precision 18, scale 2 (architecture rule).
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampAudits();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampAudits();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Maintains CreatedAt/UpdatedAt for auditable entities on every save.</summary>
    private void StampAudits()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.OnCreated(now);
                    break;
                case EntityState.Modified:
                    entry.Entity.OnUpdated(now);
                    break;
            }
        }
    }
}
