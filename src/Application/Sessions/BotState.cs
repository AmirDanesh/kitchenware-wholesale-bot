namespace KitchenwareBot.Application.Sessions;

/// <summary>All finite-state-machine states for a user's conversation with the bot.</summary>
public enum BotState
{
    // ── Customer ──────────────────────────────────────────────
    Idle = 0,
    MainMenu,
    BrowsingCategories,
    BrowsingProducts,
    ViewingProduct,
    ViewingProductAskQty, // "custom quantity" prompt
    Cart,
    CartEditQty,
    CheckoutAskDelivery,
    CheckoutAskAddress,
    CheckoutAskPayment,
    CheckoutConfirm,
    MyOrders,
    ViewingOrder,

    // ── Admin ─────────────────────────────────────────────────
    AdminMenu,

    // Products
    AdminProductList,
    AdminProductAskName,
    AdminProductAskDescription,
    AdminProductAskPrice,
    AdminProductAskCategory,
    AdminProductAskStock,
    AdminProductAskImage,
    AdminProductPreview,
    AdminProductEditAskField,
    AdminProductEditAskValue,

    // Orders
    AdminOrderList,
    AdminViewingOrder,
    AdminOrderAskNote,

    // Inventory
    AdminInventoryMenu,
    AdminAdjustStockAskProduct,
    AdminAdjustStockAskWarehouse,
    AdminAdjustStockAskQty,

    // Discounts
    AdminDiscountMenu,
    AdminGlobalDiscountList,
    AdminGlobalDiscountAskMin,
    AdminGlobalDiscountAskMax,
    AdminGlobalDiscountAskPercent,
    AdminProductDiscountList,
    AdminProductDiscountAskMin,
    AdminProductDiscountAskMax,
    AdminProductDiscountAskPercent,

    // Settings
    AdminSettings,
    AdminPaymentSettings,
    AdminBankDetailsAskName,
    AdminBankDetailsAskNumber,
    AdminBankDetailsAskHolder,
    AdminBankDetailsAskNote,
    AdminSettingsAskChannel
}
