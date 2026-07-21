using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Domain.Repositories;
using KitchenwareBot.Infrastructure.Configuration;
using KitchenwareBot.Infrastructure.Persistence;
using KitchenwareBot.Infrastructure.Persistence.Repositories;
using KitchenwareBot.Infrastructure.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace KitchenwareBot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ── EF Core / SQL Server ─────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.EnableRetryOnFailure()));

        // ── Repositories + Unit of Work ──────────────────────────
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDiscountRepository, DiscountRepository>();
        services.AddScoped<IPaymentSettingsRepository, PaymentSettingsRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Redis (bot FSM state) ────────────────────────────────
        services.Configure<RedisOptions>(configuration.GetSection("Redis"));
        var redisConnection = configuration.GetSection("Redis")["Connection"] ?? "localhost:6379";
        var redisConfig = ConfigurationOptions.Parse(redisConnection);
        redisConfig.AbortOnConnectFail = false; // stay resilient if Redis is briefly unavailable
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConfig));
        services.AddSingleton<IBotStateService, RedisBotStateService>();

        return services;
    }
}
