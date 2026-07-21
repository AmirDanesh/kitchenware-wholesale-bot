namespace KitchenwareBot.Bot.Common;

/// <summary>
/// Callback-data tokens for inline buttons. Format is colon-separated: "token:arg1:arg2".
/// Keep tokens short — Telegram limits callback_data to 64 bytes.
/// </summary>
public static class Cb
{
    // Customer
    public const string Menu = "menu";
    public const string Cats = "cats";       // reopen category list
    public const string Cat = "cat";        // cat:{categoryId}
    public const string Prod = "prod";       // prod:{productId}
    public const string ProdPage = "ppg";    // ppg:{categoryId}:{page}   (categoryId "-" = all)
    public const string AddCart = "add";     // add:{productId}:{qty}
    public const string AskQty = "aq";       // aq:{productId}
    public const string Cart = "cart";       // cart
    public const string CartClear = "cclr";  // cclr
    public const string CartDel = "cdel";    // cdel:{productId}
    public const string Checkout = "co";     // co
    public const string Delivery = "dlv";    // dlv:{deliveryTypeInt}
    public const string Pay = "pay";         // pay:{paymentMethodInt}
    public const string Confirm = "cfm";     // cfm
    public const string CheckoutCancel = "cox"; // cox
    public const string Orders = "ord";      // ord:{page}
    public const string OrderView = "vord";  // vord:{orderId}
    public const string Noop = "noop";       // inert button (e.g. page indicator)

    // Admin root: "a:{section}:..."
    public const string Admin = "a";
    public const string AdminMenu = "a:menu";
    public const string AdminProducts = "a:prods";     // a:prods:{page}
    public const string AdminProduct = "a:prod";       // a:prod:{productId}
    public const string AdminProductAdd = "a:padd";
    public const string AdminProductDel = "a:pdel";    // a:pdel:{productId}
    public const string AdminProductToggle = "a:ptog"; // a:ptog:{productId}
    public const string AdminProductPublish = "a:ppub"; // a:ppub:{productId}
    public const string AdminProductDiscount = "a:pdisc"; // a:pdisc:{productId}
    public const string AdminProductEdit = "a:ped";       // a:ped:{productId}  -> edit field menu
    public const string AdminProductEditField = "a:pef";  // a:pef:{field}      (name|desc|price); product from session
    public const string AdminProductAddCategory = "a:pac"; // a:pac:{categoryId} (add wizard)
    public const string AdminProductEditCategory = "a:pec"; // a:pec:{categoryId} (edit)
    public const string AdminProductSkipImage = "a:pski";  // skip image step during add

    public const string AdminOrders = "a:ords";        // a:ords:{status|-}:{page}
    public const string AdminOrder = "a:ord";          // a:ord:{orderId}
    public const string AdminOrderStatus = "a:ost";    // a:ost:{orderId}:{statusInt}
    public const string AdminOrderNote = "a:onote";    // a:onote:{orderId}:{statusInt}

    public const string AdminInventory = "a:inv";
    public const string AdminInvReport = "a:invr";
    public const string AdminInvLow = "a:invl";
    public const string AdminInvAdjust = "a:inva";        // a:inva  (start) then pick product
    public const string AdminInvAdjustProduct = "a:invap"; // a:invap:{productId}
    public const string AdminInvAdjustWarehouse = "a:invaw"; // a:invaw:{productId}:{warehouseId}

    public const string AdminDiscounts = "a:disc";
    public const string AdminDiscGlobal = "a:dg";        // a:dg   list global
    public const string AdminDiscGlobalAdd = "a:dga";
    public const string AdminDiscGlobalDel = "a:dgd";    // a:dgd:{tierId}
    public const string AdminDiscProductAdd = "a:dpa";   // a:dpa:{productId}
    public const string AdminDiscProductDel = "a:dpd";   // a:dpd:{tierId}:{productId}
    public const string AdminDiscProductClear = "a:dpc"; // a:dpc:{productId}

    public const string AdminSettings = "a:set";
    public const string AdminSetPayment = "a:setp";
    public const string AdminToggleBank = "a:tbank";
    public const string AdminToggleCash = "a:tcash";
    public const string AdminEditBank = "a:ebank";
    public const string AdminSetChannel = "a:chan";
    public const string AdminDiscSkipMax = "a:dmax";   // skip max quantity (unlimited tier)
    public const string AdminSkipBankNote = "a:sbn";   // skip optional bank note
    public const string AdminOrderNoteSkip = "a:ons";  // skip optional order note

    public static string Make(params object[] parts) => string.Join(":", parts);
}
