using System.Text;
using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Application.Formatting;
using KitchenwareBot.Application.Messages;
using KitchenwareBot.Application.Services;
using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Bot.Common;
using KitchenwareBot.Bot.Keyboards;

namespace KitchenwareBot.Bot.Handlers;

public class CatalogHandler : HandlerBase
{
    private readonly IProductService _products;

    public CatalogHandler(BotResponder bot, IProductService products) : base(bot) => _products = products;

    public async Task ShowCategoriesAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        var categories = await _products.GetCategoriesAsync(ct);
        ctx.Session.State = BotState.BrowsingCategories;
        if (categories.Count == 0)
        {
            await Show(ctx, BotMessages.NoCategories, null, ct);
            return;
        }
        await Show(ctx, BotMessages.ChooseCategory, CustomerKeyboards.Categories(categories), ct);
    }

    public async Task ShowProductsAsync(BotUpdateContext ctx, Guid? categoryId, int page, CancellationToken ct)
    {
        var result = await _products.GetProductsAsync(categoryId, page, Application.Common.Paging.DefaultPageSize, ct);
        ctx.Session.State = BotState.BrowsingProducts;
        ctx.Session.SelectedCategoryId = categoryId;
        ctx.Session.CurrentPage = page;

        var text = result.TotalCount == 0 ? BotMessages.NoProducts : BotMessages.ChooseProduct;
        await Show(ctx, text, CustomerKeyboards.Products(result.Items, categoryId, page, result.TotalPages), ct);
    }

    public async Task ShowProductDetailAsync(BotUpdateContext ctx, Guid productId, CancellationToken ct)
    {
        var dto = await _products.GetProductDetailAsync(productId, ct);
        if (dto is null || !dto.IsActive)
        {
            await Answer(ctx, BotMessages.NothingHere, alert: true, ct: ct);
            return;
        }

        ctx.Session.State = BotState.ViewingProduct;
        ctx.Session.SelectedProductId = productId;
        ctx.Session.SelectedCategoryId = dto.CategoryId;

        var caption = BuildCaption(dto);
        var canOrder = dto.AvailableStock > 0;
        var keyboard = CustomerKeyboards.ProductDetail(dto.Id, dto.CategoryId, canOrder);

        // Always send a fresh message (a photo message cannot be produced by editing a text one).
        if (!string.IsNullOrWhiteSpace(dto.TelegramFileId))
            await SendPhoto(ctx, dto.TelegramFileId!, caption, keyboard, ct);
        else
            await Send(ctx, caption, keyboard, ct);

        await Answer(ctx, ct: ct);
    }

    private static string BuildCaption(ProductDetailDto dto)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🍳 {dto.Name}");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(dto.Description))
        {
            sb.AppendLine(dto.Description);
            sb.AppendLine();
        }
        sb.AppendLine($"💰 قیمت واحد: {PriceFormatter.FormatToman(dto.Price)}");
        sb.AppendLine(StockLine(dto));
        sb.AppendLine();
        sb.AppendLine(BuildDiscountTable(dto.DiscountTiers));
        return sb.ToString().TrimEnd();
    }

    private static string StockLine(ProductDetailDto dto)
    {
        if (dto.AvailableStock <= 0) return BotMessages.StockOut;
        var label = dto.IsLowStock ? BotMessages.StockLow : BotMessages.StockAvailable;
        return $"{label} ({PriceFormatter.FormatNumber(dto.AvailableStock)})";
    }

    public static string BuildDiscountTable(IReadOnlyList<DiscountTierDto> tiers)
    {
        if (tiers.Count == 0) return $"{BotMessages.DiscountTableHeader}\n{BotMessages.NoDiscount}";

        var sb = new StringBuilder();
        sb.AppendLine(BotMessages.DiscountTableHeader);
        foreach (var t in tiers)
        {
            var qty = t.MaxQuantity.HasValue
                ? $"{PriceFormatter.FormatNumber(t.MinQuantity)} تا {PriceFormatter.FormatNumber(t.MaxQuantity.Value)} عدد"
                : $"{PriceFormatter.FormatNumber(t.MinQuantity)} عدد و بیشتر";
            sb.AppendLine($"{qty}: {PriceFormatter.FormatPercent(t.DiscountPercent)} تخفیف");
        }
        return sb.ToString().TrimEnd();
    }
}
