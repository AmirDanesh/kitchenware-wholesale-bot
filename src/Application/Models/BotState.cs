namespace KitchenwareBot.Application.Models;

public enum BotState
{
    Idle,
    MainMenu,

    // Customer — catalog
    BrowsingCategories,
    BrowsingProducts,
    ViewingProduct,

    // Customer — cart & checkout
    Cart,
    CheckoutAskDelivery,
    CheckoutAskAddress,
    CheckoutAskPayment,
    CheckoutConfirm,

    // Customer — orders
    MyOrders,
    ViewingOrder,

    // Admin — main
    AdminMenu,

    // Admin — products
    AdminProductList,
    AdminProductAskName,
    AdminProductAskDescription,
    AdminProductAskPrice,
    AdminProductAskCategory,
    AdminProductAskStock,
    AdminProductAskImage,
    AdminProductPreview,

    // Admin — orders
    AdminOrderList,
    AdminViewingOrder,
    AdminOrderAskNote,

    // Admin — inventory
    AdminInventoryMenu,
    AdminAdjustStockAskProduct,
    AdminAdjustStockAskQty,

    // Admin — discounts
    AdminDiscountMenu,
    AdminGlobalDiscountList,
    AdminGlobalDiscountAskMin,
    AdminGlobalDiscountAskMax,
    AdminGlobalDiscountAskPercent,
    AdminProductDiscountList,
    AdminProductDiscountAskMin,
    AdminProductDiscountAskMax,
    AdminProductDiscountAskPercent,

    // Admin — settings
    AdminSettings,
    AdminPaymentSettings,
    AdminBankDetails
}
