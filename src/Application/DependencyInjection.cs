using FluentValidation;
using KitchenwareBot.Application.Configuration;
using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Application.Services;
using KitchenwareBot.Application.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KitchenwareBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Admin failsafe IDs come from the Telegram config section.
        services.Configure<AdminOptions>(configuration.GetSection("Telegram"));

        // Validators
        services.AddScoped<IValidator<CreateProductDto>, CreateProductDtoValidator>();
        services.AddScoped<IValidator<TierInputDto>, TierInputDtoValidator>();

        // Services (the shared integration point for both the bot and the future API)
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IDiscountService, DiscountService>();
        services.AddScoped<IPaymentSettingsService, PaymentSettingsService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
