using KitchenwareBot.Domain.Enums;

namespace KitchenwareBot.Application.Messages;

/// <summary>
/// All user-facing Persian strings, in one place for easy editing. Handlers must reference these
/// rather than hard-coding Persian literals. Dynamic messages are composed from these + PriceFormatter.
/// </summary>
public static class BotMessages
{
    // ── Common / errors ───────────────────────────────────────────
    public const string Welcome = "سلام {0} عزیز! 👋\nبه فروشگاه عمده‌فروشی لوازم آشپزخانه خوش آمدید.";
    public const string MainMenu = "از منوی زیر انتخاب کنید:";
    public const string GenericError = "❌ خطایی رخ داد. لطفاً دوباره تلاش کنید.";
    public const string ShopClosed = "🔒 فروشگاه در حال حاضر تعطیل می‌باشد.";
    public const string OutOfStock = "❌ موجودی کافی نیست.";
    public const string Unauthorized = "⛔ شما دسترسی لازم را ندارید.";
    public const string Banned = "⛔ حساب شما مسدود شده است. برای پیگیری با پشتیبانی تماس بگیرید.";
    public const string SessionExpired = "⏱ نشست شما منقضی شد. برای شروع دوباره /start را بزنید.";
    public const string InvalidNumber = "لطفاً یک عدد معتبر وارد کنید.";
    public const string OperationCancelled = "عملیات لغو شد.";
    public const string NothingHere = "موردی برای نمایش وجود ندارد.";
    public const string Help = "🛍 برای مشاهده محصولات از دکمه «محصولات» استفاده کنید.\n🛒 اقلام انتخابی در «سبد خرید» نگهداری می‌شوند.\n📦 وضعیت سفارش‌ها در «سفارش‌های من» قابل مشاهده است.";

    // ── Main menu buttons (customer) ───────────────────────────────
    public const string BtnCatalog = "🛍 محصولات";
    public const string BtnCart = "🛒 سبد خرید";
    public const string BtnMyOrders = "📦 سفارش‌های من";
    public const string BtnHelp = "ℹ️ راهنما";

    // ── Navigation / confirm ───────────────────────────────────────
    public const string BtnBack = "◄ بازگشت";
    public const string BtnPrevPage = "◄ صفحه قبل";
    public const string BtnNextPage = "صفحه بعد ►";
    public const string BtnConfirm = "✅ تأیید";
    public const string BtnCancel = "❌ انصراف";
    public const string BtnYes = "بله";
    public const string BtnNo = "خیر";
    public const string BtnSkip = "رد کردن";
    public const string BtnMainMenu = "🏠 منوی اصلی";

    // ── Catalog / product ──────────────────────────────────────────
    public const string ChooseCategory = "یک دسته‌بندی را انتخاب کنید:";
    public const string NoCategories = "هنوز دسته‌بندی‌ای ثبت نشده است.";
    public const string NoProducts = "محصولی در این دسته‌بندی موجود نیست.";
    public const string ChooseProduct = "یک محصول را انتخاب کنید:";
    public const string DiscountTableHeader = "📊 جدول تخفیف:";
    public const string NoDiscount = "بدون تخفیف";
    public const string StockAvailable = "✅ موجود";
    public const string StockLow = "⚠️ موجودی محدود";
    public const string StockOut = "❌ ناموجود";
    public const string BtnAddToCart = "🛒 افزودن به سبد";
    public const string BtnCustomQty = "🔢 تعداد دلخواه";
    public const string AskCustomQty = "تعداد مورد نظر را وارد کنید:";
    public const string AddedToCart = "✅ به سبد خرید اضافه شد.";

    // ── Cart ───────────────────────────────────────────────────────
    public const string CartEmpty = "🛒 سبد خرید شما خالی است.";
    public const string CartHeader = "🛒 سبد خرید شما:";
    public const string BtnCheckout = "✅ تسویه‌حساب";
    public const string BtnEditCart = "✏️ ویرایش";
    public const string BtnClearCart = "🗑 پاک کردن سبد";
    public const string CartCleared = "سبد خرید پاک شد.";
    public const string BtnRemoveItem = "حذف";

    // ── Checkout ───────────────────────────────────────────────────
    public const string AskDelivery = "نحوه دریافت سفارش را انتخاب کنید:";
    public const string AskAddress = "لطفاً آدرس کامل پستی خود را ارسال کنید:";
    public const string AskPayment = "روش پرداخت را انتخاب کنید:";
    public const string ConfirmOrderPrompt = "سفارش خود را بررسی و تأیید کنید:";
    public const string CheckoutExpired = "⏱ اطلاعات تسویه‌حساب منقضی شده است. لطفاً دوباره از سبد خرید شروع کنید.";
    public const string CheckoutAlreadyProcessing = "⏳ سفارش شما در حال ثبت است. لطفاً منتظر بمانید.";
    public const string ProductUnavailable = "❌ یکی از محصولات سبد خرید دیگر قابل سفارش نیست.\n🍳 {0}";
    public const string OrderPlaced = "✅ سفارش شما با کد {0} ثبت شد.\nپس از بررسی توسط فروشنده، وضعیت سفارش به شما اطلاع داده می‌شود.";
    public const string BankInstructionsHeader = "💳 اطلاعات واریز:";
    public const string BtnDeliveryShipping = "🚚 ارسال پستی";
    public const string BtnDeliveryInPerson = "🏬 تحویل حضوری";
    public const string BtnPayBank = "💳 کارت به کارت / واریز بانکی";
    public const string BtnPayCash = "💵 نقدی";

    // ── My orders ──────────────────────────────────────────────────
    public const string MyOrdersHeader = "📦 سفارش‌های شما:";
    public const string NoOrders = "شما هنوز سفارشی ثبت نکرده‌اید.";

    // ── Admin: menu ────────────────────────────────────────────────
    public const string AdminWelcome = "🔐 پنل مدیریت";
    public const string AdminBtnProducts = "📦 محصولات";
    public const string AdminBtnOrders = "🧾 سفارش‌ها";
    public const string AdminBtnInventory = "🏭 موجودی انبار";
    public const string AdminBtnDiscounts = "🏷 تخفیف‌ها";
    public const string AdminBtnSettings = "⚙️ تنظیمات";

    // ── Admin: products ────────────────────────────────────────────
    public const string AdminBtnAddProduct = "➕ افزودن محصول";
    public const string AdminAskProductName = "نام محصول را وارد کنید:";
    public const string AdminAskProductDescription = "توضیحات محصول را وارد کنید:";
    public const string AdminAskProductPrice = "قیمت واحد (تومان) را وارد کنید:";
    public const string AdminAskProductCategory = "دسته‌بندی محصول را انتخاب کنید:";
    public const string AdminAskProductStock = "موجودی اولیه را وارد کنید:";
    public const string AdminAskProductImage = "تصویر محصول را ارسال کنید یا «رد کردن» را بزنید:";
    public const string AdminProductSaved = "✅ محصول با موفقیت ذخیره شد.";
    public const string AdminProductUpdated = "✅ محصول به‌روزرسانی شد.";
    public const string AdminProductDeleted = "✅ محصول حذف (غیرفعال) شد.";
    public const string AdminBtnEdit = "✏️ ویرایش";
    public const string AdminBtnDelete = "🗑 حذف";
    public const string AdminBtnPublish = "📢 انتشار در کانال";
    public const string AdminBtnSetDiscount = "🏷 تنظیم تخفیف";
    public const string AdminBtnToggleActive = "🔁 فعال/غیرفعال";
    public const string AdminAskPublish = "این محصول در کانال منتشر شود؟";
    public const string AdminPublished = "✅ در کانال منتشر شد.";
    public const string AdminPublishFailed = "❌ انتشار در کانال ناموفق بود. تنظیمات کانال را بررسی کنید.";
    public const string AdminChannelNotConfigured = "شناسه کانال تنظیم نشده است.";

    // ── Admin: orders ──────────────────────────────────────────────
    public const string AdminOrdersHeader = "🧾 سفارش‌ها:";
    public const string AdminAskOrderNote = "یادداشت (اختیاری) را وارد کنید یا «رد کردن» را بزنید:";
    public const string AdminOrderStatusUpdated = "✅ وضعیت سفارش به‌روزرسانی شد.";
    public const string AdminBtnFilterAll = "همه";

    // ── Admin: inventory ───────────────────────────────────────────
    public const string AdminInventoryMenu = "🏭 مدیریت موجودی:";
    public const string AdminBtnStockReport = "📋 گزارش موجودی";
    public const string AdminBtnLowStock = "⚠️ کالاهای رو به اتمام";
    public const string AdminBtnAdjustStock = "➕➖ تغییر موجودی";
    public const string AdminBtnWarehouses = "🏬 انبارها";
    public const string AdminAskAdjustProduct = "محصول را انتخاب کنید:";
    public const string AdminAskAdjustWarehouse = "انبار را انتخاب کنید:";
    public const string AdminAskAdjustQty = "مقدار تغییر را وارد کنید (مثبت برای افزایش، منفی برای کاهش):";
    public const string AdminStockAdjusted = "✅ موجودی به‌روزرسانی شد.";
    public const string AdminNoLowStock = "✅ هیچ کالایی رو به اتمام نیست.";

    // ── Admin: discounts ───────────────────────────────────────────
    public const string AdminDiscountMenu = "🏷 مدیریت تخفیف‌ها:";
    public const string AdminBtnGlobalTiers = "🌐 تخفیف‌های عمومی";
    public const string AdminBtnAddTier = "➕ افزودن پله تخفیف";
    public const string AdminAskTierMin = "حداقل تعداد را وارد کنید:";
    public const string AdminAskTierMax = "حداکثر تعداد را وارد کنید (برای نامحدود «رد کردن» را بزنید):";
    public const string AdminAskTierPercent = "درصد تخفیف را وارد کنید:";
    public const string AdminTierSaved = "✅ پله تخفیف ذخیره شد.";
    public const string AdminTierDeleted = "✅ پله تخفیف حذف شد.";
    public const string AdminBtnUseGlobal = "حذف تخفیف اختصاصی و استفاده از تخفیف عمومی";
    public const string AdminProductTiersCleared = "✅ تخفیف اختصاصی حذف شد؛ از تخفیف عمومی استفاده می‌شود.";

    // ── Admin: settings ────────────────────────────────────────────
    public const string AdminSettingsMenu = "⚙️ تنظیمات فروشگاه:";
    public const string AdminBtnPaymentSettings = "💳 روش‌های پرداخت";
    public const string AdminBtnChannelSettings = "📢 تنظیم کانال";
    public const string AdminBtnToggleBank = "کارت به کارت";
    public const string AdminBtnToggleCash = "نقدی";
    public const string AdminBtnEditBankDetails = "✏️ ویرایش اطلاعات بانکی";
    public const string AdminAskBankName = "نام بانک را وارد کنید:";
    public const string AdminAskBankNumber = "شماره کارت/حساب را وارد کنید:";
    public const string AdminAskBankHolder = "نام صاحب حساب را وارد کنید:";
    public const string AdminAskBankNote = "یادداشت برای مشتری را وارد کنید (اختیاری):";
    public const string AdminSettingsSaved = "✅ تنظیمات ذخیره شد.";
    public const string AdminAskChannelId = "شناسه کانال را وارد کنید (مثال: ‎-1001234567890):";
    public const string ShopStatusOpen = "وضعیت فروشگاه: باز 🟢";
    public const string ShopStatusClosed = "وضعیت فروشگاه: بسته 🔴";
    public const string Enabled = "فعال ✅";
    public const string Disabled = "غیرفعال ❌";

    // ── Admin notifications ────────────────────────────────────────
    public const string AdminNewOrderHeader = "🔔 سفارش جدید ثبت شد!";
    public const string AdminLowStockHeader = "⚠️ هشدار موجودی کم:";

    // ── Customer status notifications ──────────────────────────────
    public const string NotifyConfirmed = "✅ سفارش شما تأیید شد.";
    public const string NotifyProcessing = "🔄 سفارش شما در حال پردازش است.";
    public const string NotifyShipped = "🚚 سفارش شما ارسال شد.";
    public const string NotifyDelivered = "✔️ سفارش شما تحویل داده شد.";
    public const string NotifyCancelled = "❌ سفارش شما لغو شد.";

    // ── Label helpers ──────────────────────────────────────────────
    public static string OrderStatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "در انتظار تأیید",
        OrderStatus.Confirmed => "تأیید شده",
        OrderStatus.Processing => "در حال پردازش",
        OrderStatus.Shipped => "ارسال شده",
        OrderStatus.Delivered => "تحویل داده شده",
        OrderStatus.Cancelled => "لغو شده",
        _ => status.ToString()
    };

    public static string PaymentLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.BankTransfer => "کارت به کارت / واریز بانکی",
        PaymentMethod.Cash => "نقدی",
        _ => method.ToString()
    };

    public static string DeliveryLabel(DeliveryType type) => type switch
    {
        DeliveryType.Shipping => "ارسال پستی",
        DeliveryType.InPerson => "تحویل حضوری",
        _ => type.ToString()
    };

    /// <summary>The customer-facing status-change line (note appended by caller when present).</summary>
    public static string StatusNotification(OrderStatus status) => status switch
    {
        OrderStatus.Confirmed => NotifyConfirmed,
        OrderStatus.Processing => NotifyProcessing,
        OrderStatus.Shipped => NotifyShipped,
        OrderStatus.Delivered => NotifyDelivered,
        OrderStatus.Cancelled => NotifyCancelled,
        _ => $"وضعیت سفارش شما: {OrderStatusLabel(status)}"
    };
}
