using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KitchenwareBot.Application.Abstractions;
using KitchenwareBot.Application;
using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Application.Services;
using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Domain.Enums;
using KitchenwareBot.Infrastructure;
using KitchenwareBot.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<INotificationService, NullNotificationService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api");

api.MapGet("/catalog/categories", async (IProductService products, CancellationToken ct) =>
    Results.Ok((await products.GetCategoriesAsync(ct)).Select(x => new CategoryResponse(x.Id, x.Name, x.ParentId))));

api.MapGet("/catalog/products", async (Guid? categoryId, string? search, int page, IProductService products, CancellationToken ct) =>
{
    page = Math.Max(page, 1);
    if (!string.IsNullOrWhiteSpace(search))
    {
        var searchResult = await products.SearchProductsAsync(search.Trim(), ct);
        return Results.Ok(new { items = searchResult.Select(ToProductCard), page = 1, totalPages = 1, totalCount = searchResult.Count });
    }

    var result = await products.GetProductsAsync(categoryId, page, 20, ct);
    return Results.Ok(new { items = result.Items.Select(ToProductCard), result.Page, result.TotalPages, result.TotalCount });
});

api.MapGet("/catalog/products/{id:guid}", async (Guid id, IProductService products, CancellationToken ct) =>
{
    var product = await products.GetProductDetailAsync(id, ct);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

api.MapPost("/orders/preview", async (CheckoutRequest request, IOrderService orders, CancellationToken ct) =>
{
    var cart = request.Items.Select(x => new CartItem { ProductId = x.ProductId, Quantity = x.Quantity }).ToList();
    return Results.Ok(await orders.CalculateOrderAsync(cart, ct));
});

api.MapPost("/orders", async (HttpRequest httpRequest, CheckoutRequest request, IOrderService orders, IUserService users, IConfiguration configuration, CancellationToken ct) =>
{
    var telegramUser = TelegramIdentity.TryRead(httpRequest, configuration);
    if (telegramUser is null) return Results.Unauthorized();

    var user = await users.GetOrCreateAsync(telegramUser.Id, telegramUser.Username, telegramUser.FirstName, ct);
    if (user.IsBanned) return Results.Forbid();

    var draft = new OrderDraft
    {
        CheckoutToken = Guid.NewGuid(),
        Payment = request.PaymentMethod,
        Delivery = request.DeliveryType,
        Address = request.Address
    };
    var cart = request.Items.Select(x => new CartItem { ProductId = x.ProductId, Quantity = x.Quantity }).ToList();
    var order = await orders.PlaceOrderAsync(telegramUser.Id, telegramUser.FirstName ?? "مشتری", request.Phone, cart, draft, ct);
    return Results.Created($"/api/orders/{order.OrderId}", order);
});

api.MapGet("/orders/mine", async (HttpRequest httpRequest, IOrderService orders, IConfiguration configuration, CancellationToken ct) =>
{
    var telegramUser = TelegramIdentity.TryRead(httpRequest, configuration);
    if (telegramUser is null) return Results.Unauthorized();

    var result = await orders.GetCustomerOrdersAsync(telegramUser.Id, 1, 50, ct);
    return Results.Ok(result.Items.Select(ToOrder));
});

var admin = api.MapGroup("/admin").AddEndpointFilter<AdminFilter>();

admin.MapGet("/products", async (IProductService products, CancellationToken ct) =>
{
    var result = await products.GetAllProductsAsync(1, 100, ct);
    return Results.Ok(result.Items.Select(ToProductCard));
});

admin.MapPost("/products", async (CreateProductDto dto, IProductService products, CancellationToken ct) =>
{
    var id = await products.CreateProductAsync(dto, ct);
    return Results.Created($"/api/catalog/products/{id}", new { id });
});

admin.MapPatch("/products/{id:guid}/active", async (Guid id, IProductService products, CancellationToken ct) =>
    Results.Ok(new { isActive = await products.ToggleActiveAsync(id, ct) }));

admin.MapGet("/orders", async (IOrderService orders, CancellationToken ct) =>
{
    var result = await orders.GetAllOrdersAsync(null, 1, 100, ct);
    return Results.Ok(result.Items.Select(ToOrder));
});

admin.MapPatch("/orders/{id:guid}/status", async (Guid id, UpdateOrderStatusRequest request, IOrderService orders, CancellationToken ct) =>
{
    if (request.Status == OrderStatus.Cancelled)
        await orders.CancelOrderAsync(id, request.Note, ct);
    else
        await orders.UpdateOrderStatusAsync(id, request.Status, request.Note, ct);
    return Results.NoContent();
});

app.MapFallbackToFile("index.html");
app.Run();

static ProductCardResponse ToProductCard(KitchenwareBot.Domain.Entities.Product product) =>
    new(product.Id, product.Name, product.Description, product.Price, product.CategoryId, product.ImagePath, product.IsActive);

static OrderResponse ToOrder(KitchenwareBot.Domain.Entities.Order order) => new(
    order.Id, order.ShortCode, order.CustomerName, order.CustomerPhone, order.Status, order.PaymentMethod,
    order.DeliveryType, order.ShippingAddress, order.AdminNote, order.TotalAmount, order.CreatedAt,
    order.Items.Select(x => new OrderItemResponse(x.ProductName, x.Quantity, x.OriginalPrice, x.DiscountPercent, x.UnitPrice, x.SubTotal)));

public record CategoryResponse(Guid Id, string Name, Guid? ParentId);
public record ProductCardResponse(Guid Id, string Name, string Description, decimal Price, Guid CategoryId, string? ImagePath, bool IsActive);
public record CartLineRequest(Guid ProductId, int Quantity);
public record CheckoutRequest(IReadOnlyList<CartLineRequest> Items, PaymentMethod PaymentMethod, DeliveryType DeliveryType, string? Address, string? Phone);
public record UpdateOrderStatusRequest(OrderStatus Status, string? Note);
public record OrderItemResponse(string Name, int Quantity, decimal OriginalPrice, decimal DiscountPercent, decimal UnitPrice, decimal SubTotal);
public record OrderResponse(Guid Id, string ShortCode, string CustomerName, string? CustomerPhone, OrderStatus Status, PaymentMethod PaymentMethod, DeliveryType DeliveryType, string? Address, string? Note, decimal Total, DateTime CreatedAt, IEnumerable<OrderItemResponse> Items);

public sealed class AdminFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var identity = TelegramIdentity.TryRead(context.HttpContext.Request, configuration);
        if (identity is null) return Results.Unauthorized();

        var users = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
        if (!await users.IsAdminAsync(identity.Id, context.HttpContext.RequestAborted)) return Results.Forbid();
        return await next(context);
    }
}

public sealed record TelegramUser(long Id, string? Username, string? FirstName);

/// <summary>API host has no Telegram client; Bot host owns delivery of Telegram notifications.</summary>
public sealed class NullNotificationService : INotificationService
{
    public Task NotifyAdminsNewOrderAsync(KitchenwareBot.Domain.Entities.Order order, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotifyAdminsLowStockAsync(KitchenwareBot.Application.DTOs.LowStockItemDto item, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotifyCustomerOrderStatusAsync(KitchenwareBot.Domain.Entities.Order order, CancellationToken ct = default) => Task.CompletedTask;
}

public static class TelegramIdentity
{
    public static TelegramUser? TryRead(HttpRequest request, IConfiguration configuration)
    {
        var initData = request.Headers["X-Telegram-Init-Data"].ToString();
        var botToken = configuration["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(initData) || string.IsNullOrWhiteSpace(botToken))
        {
            return long.TryParse(configuration["MiniApp:LocalDevelopmentTelegramId"], out var localId)
                ? new TelegramUser(localId, null, configuration["MiniApp:LocalDevelopmentName"] ?? "کاربر آزمایشی")
                : null;
        }

        var fields = initData.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Split('=', 2))
            .Where(x => x.Length == 2)
            .ToDictionary(x => Uri.UnescapeDataString(x[0]), x => Uri.UnescapeDataString(x[1]), StringComparer.Ordinal);
        if (!fields.Remove("hash", out var suppliedHash) || !fields.TryGetValue("auth_date", out var authDate) || !fields.TryGetValue("user", out var userJson)) return null;
        if (!long.TryParse(authDate, out var seconds) || DateTimeOffset.FromUnixTimeSeconds(seconds) < DateTimeOffset.UtcNow.AddDays(-1)) return null;

        var checkString = string.Join("\n", fields.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => $"{x.Key}={x.Value}"));
        var secret = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(botToken));
        var calculatedHash = Convert.ToHexString(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(checkString))).ToLowerInvariant();
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(calculatedHash), Encoding.UTF8.GetBytes(suppliedHash))) return null;

        using var user = JsonDocument.Parse(userJson);
        var root = user.RootElement;
        return root.TryGetProperty("id", out var id) && id.TryGetInt64(out var telegramId)
            ? new TelegramUser(telegramId, root.TryGetProperty("username", out var username) ? username.GetString() : null, root.TryGetProperty("first_name", out var firstName) ? firstName.GetString() : null)
            : null;
    }
}
