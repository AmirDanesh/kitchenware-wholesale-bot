using System.Text;
using KitchenwareBot.Application.Common;
using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Application.Formatting;
using KitchenwareBot.Application.Messages;
using KitchenwareBot.Application.Services;
using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Bot.Common;
using KitchenwareBot.Bot.Keyboards;

namespace KitchenwareBot.Bot.Handlers;

public class ProductAdminHandler : HandlerBase
{
    private readonly IProductService _products;

    public ProductAdminHandler(BotResponder bot, IProductService products) : base(bot) => _products = products;

    // ── List / view ───────────────────────────────────────────────
    public async Task ShowListAsync(BotUpdateContext ctx, int page, CancellationToken ct)
    {
        var result = await _products.GetAllProductsAsync(page, Paging.DefaultPageSize, ct);
        ctx.Session.State = BotState.AdminProductList;
        await Answer(ctx, ct: ct);
        var text = result.TotalCount == 0 ? BotMessages.NothingHere : BotMessages.AdminBtnProducts;
        await Show(ctx, text, AdminKeyboards.ProductList(result.Items, page, result.TotalPages), ct);
    }

    public async Task ShowProductAsync(BotUpdateContext ctx, Guid productId, CancellationToken ct)
    {
        var dto = await _products.GetProductDetailAsync(productId, ct);
        if (dto is null)
        {
            await Answer(ctx, BotMessages.NothingHere, alert: true, ct: ct);
            return;
        }
        ctx.Session.SelectedProductId = productId;
        await Answer(ctx, ct: ct);

        var sb = new StringBuilder();
        sb.AppendLine($"🍳 {dto.Name} {(dto.IsActive ? "🟢" : "⚪️")}");
        sb.AppendLine($"دسته: {dto.CategoryName}");
        sb.AppendLine($"قیمت: {PriceFormatter.FormatToman(dto.Price)}");
        sb.AppendLine($"موجودی: {PriceFormatter.FormatNumber(dto.AvailableStock)}");
        if (!string.IsNullOrWhiteSpace(dto.Description)) sb.AppendLine($"\n{dto.Description}");
        await Show(ctx, sb.ToString().TrimEnd(), AdminKeyboards.ProductActions(dto.Id, dto.IsActive), ct);
    }

    // ── Add wizard ────────────────────────────────────────────────
    public async Task StartAddAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        ctx.Session.ProductDraft = new ProductDraft();
        ctx.Session.State = BotState.AdminProductAskName;
        await Answer(ctx, ct: ct);
        await Show(ctx, BotMessages.AdminAskProductName, null, ct);
    }

    public async Task HandleTextAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        var draft = ctx.Session.ProductDraft ??= new ProductDraft();
        var text = ctx.Text?.Trim() ?? string.Empty;

        switch (ctx.Session.State)
        {
            case BotState.AdminProductAskName:
                if (string.IsNullOrWhiteSpace(text)) { await Send(ctx, BotMessages.AdminAskProductName, ct: ct); return; }
                draft.Name = text;
                ctx.Session.State = BotState.AdminProductAskDescription;
                await Send(ctx, BotMessages.AdminAskProductDescription, ct: ct);
                break;

            case BotState.AdminProductAskDescription:
                draft.Description = text;
                ctx.Session.State = BotState.AdminProductAskPrice;
                await Send(ctx, BotMessages.AdminAskProductPrice, ct: ct);
                break;

            case BotState.AdminProductAskPrice:
                if (!PriceFormatter.TryParseDecimal(text, out var price) || price < 0) { await Send(ctx, BotMessages.InvalidNumber, ct: ct); return; }
                draft.Price = price;
                ctx.Session.State = BotState.AdminProductAskCategory;
                var categories = await _products.GetCategoriesAsync(ct);
                await Send(ctx, BotMessages.AdminAskProductCategory, AdminKeyboards.CategoryPicker(categories, Cb.AdminProductAddCategory), ct);
                break;

            case BotState.AdminProductAskStock:
                if (!PriceFormatter.TryParseInt(text, out var stock) || stock < 0) { await Send(ctx, BotMessages.InvalidNumber, ct: ct); return; }
                draft.Stock = stock;
                ctx.Session.State = BotState.AdminProductAskImage;
                await Send(ctx, BotMessages.AdminAskProductImage, AdminKeyboards.Skip(Cb.AdminProductSkipImage), ct);
                break;

            case BotState.AdminProductEditAskValue:
                await ApplyEditValueAsync(ctx, text, ct);
                break;
        }
    }

    public async Task HandleAddCategoryAsync(BotUpdateContext ctx, Guid categoryId, CancellationToken ct)
    {
        var draft = ctx.Session.ProductDraft ??= new ProductDraft();
        draft.CategoryId = categoryId;
        ctx.Session.State = BotState.AdminProductAskStock;
        await Answer(ctx, ct: ct);
        await Show(ctx, BotMessages.AdminAskProductStock, null, ct);
    }

    public async Task HandlePhotoAsync(BotUpdateContext ctx, string fileId, CancellationToken ct)
    {
        var draft = ctx.Session.ProductDraft ??= new ProductDraft();
        draft.TelegramFileId = fileId;
        await FinalizeAddAsync(ctx, ct);
    }

    public async Task SkipImageAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        await Answer(ctx, ct: ct);
        await FinalizeAddAsync(ctx, ct);
    }

    private async Task FinalizeAddAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        var draft = ctx.Session.ProductDraft;
        if (draft?.Name is null || draft.Price is null || draft.CategoryId is null)
        {
            await Send(ctx, BotMessages.GenericError, ct: ct);
            ctx.Session.State = BotState.AdminMenu;
            return;
        }

        var dto = new CreateProductDto(draft.Name, draft.Description, draft.Price.Value, draft.CategoryId.Value,
            draft.Stock ?? 0, draft.TelegramFileId);
        var productId = await _products.CreateProductAsync(dto, ct);

        ctx.Session.ProductDraft = null;
        ctx.Session.SelectedProductId = productId;
        ctx.Session.State = BotState.AdminProductPreview;

        await Send(ctx, BotMessages.AdminProductSaved, ct: ct);
        await Send(ctx, BotMessages.AdminAskPublish,
            AdminKeyboards.YesNo(Cb.Make(Cb.AdminProductPublish, productId), Cb.Make(Cb.AdminProduct, productId)), ct);
    }

    // ── Edit ──────────────────────────────────────────────────────
    public async Task ShowEditMenuAsync(BotUpdateContext ctx, Guid productId, CancellationToken ct)
    {
        ctx.Session.SelectedProductId = productId;
        await Answer(ctx, ct: ct);
        await Show(ctx, BotMessages.AdminBtnEdit, AdminKeyboards.EditFieldMenu(), ct);
    }

    public async Task StartEditFieldAsync(BotUpdateContext ctx, string field, CancellationToken ct)
    {
        if (ctx.Session.SelectedProductId is null) { await Answer(ctx, BotMessages.NothingHere, alert: true, ct: ct); return; }

        if (field == "cat")
        {
            var categories = await _products.GetCategoriesAsync(ct);
            await Answer(ctx, ct: ct);
            await Show(ctx, BotMessages.AdminAskProductCategory, AdminKeyboards.CategoryPicker(categories, Cb.AdminProductEditCategory), ct);
            return;
        }

        ctx.Session.EditField = field;
        ctx.Session.State = BotState.AdminProductEditAskValue;
        var prompt = field switch
        {
            "name" => BotMessages.AdminAskProductName,
            "desc" => BotMessages.AdminAskProductDescription,
            "price" => BotMessages.AdminAskProductPrice,
            _ => BotMessages.AdminAskProductName
        };
        await Answer(ctx, ct: ct);
        await Show(ctx, prompt, null, ct);
    }

    private async Task ApplyEditValueAsync(BotUpdateContext ctx, string text, CancellationToken ct)
    {
        if (ctx.Session.SelectedProductId is not { } productId) { ctx.Session.State = BotState.AdminMenu; return; }
        var current = await _products.GetProductDetailAsync(productId, ct);
        if (current is null) { await Send(ctx, BotMessages.NothingHere, ct: ct); return; }

        string name = current.Name, desc = current.Description;
        decimal price = current.Price;
        var categoryId = current.CategoryId;

        switch (ctx.Session.EditField)
        {
            case "name":
                if (string.IsNullOrWhiteSpace(text)) { await Send(ctx, BotMessages.AdminAskProductName, ct: ct); return; }
                name = text; break;
            case "desc":
                desc = text; break;
            case "price":
                if (!PriceFormatter.TryParseDecimal(text, out var p) || p < 0) { await Send(ctx, BotMessages.InvalidNumber, ct: ct); return; }
                price = p; break;
        }

        await _products.UpdateProductAsync(productId, new UpdateProductDto(name, desc, price, categoryId), ct);
        ctx.Session.EditField = null;
        ctx.Session.State = BotState.AdminMenu;
        await Send(ctx, BotMessages.AdminProductUpdated, ct: ct);
        await ShowProductAsync(ctx, productId, ct);
    }

    public async Task HandleEditCategoryAsync(BotUpdateContext ctx, Guid categoryId, CancellationToken ct)
    {
        if (ctx.Session.SelectedProductId is not { } productId) { await Answer(ctx, BotMessages.NothingHere, alert: true, ct: ct); return; }
        var current = await _products.GetProductDetailAsync(productId, ct);
        if (current is null) { await Answer(ctx, BotMessages.NothingHere, alert: true, ct: ct); return; }
        await _products.UpdateProductAsync(productId, new UpdateProductDto(current.Name, current.Description, current.Price, categoryId), ct);
        await Answer(ctx, BotMessages.AdminProductUpdated, ct: ct);
        await ShowProductAsync(ctx, productId, ct);
    }

    // ── Toggle / delete ───────────────────────────────────────────
    public async Task ToggleActiveAsync(BotUpdateContext ctx, Guid productId, CancellationToken ct)
    {
        await _products.ToggleActiveAsync(productId, ct);
        await Answer(ctx, BotMessages.AdminProductUpdated, ct: ct);
        await ShowProductAsync(ctx, productId, ct);
    }

    public async Task DeleteAsync(BotUpdateContext ctx, Guid productId, bool confirmed, CancellationToken ct)
    {
        if (!confirmed)
        {
            await Answer(ctx, ct: ct);
            await Show(ctx, $"{BotMessages.AdminBtnDelete}؟",
                AdminKeyboards.YesNo(Cb.Make(Cb.AdminProductDel, productId, 1), Cb.Make(Cb.AdminProduct, productId)), ct);
            return;
        }
        await _products.DeleteProductAsync(productId, ct);
        await Answer(ctx, BotMessages.AdminProductDeleted, ct: ct);
        await ShowListAsync(ctx, 1, ct);
    }
}
