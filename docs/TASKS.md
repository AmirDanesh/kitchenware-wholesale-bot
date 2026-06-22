# 📋 KitchenwareBot — Implementation Task Plan
> Stack: .NET 8 · SQL Server · Telegram.Bot · Redis · Webhook · Persian/Toman

---

## Architecture Decisions (Final)

| Question | Decision |
|----------|----------|
| MediatR | ❌ Dropped. Using plain **Service Classes** + DI. Simpler, zero cost, same result. |
| Database | ✅ **SQL Server** — `LocalDB` for dev, `SQL Server Express` (free) for production |
| Language | ✅ **Persian only** — all bot messages in Farsi, prices in Toman format |
| Bot mode | ✅ **Webhook** |
| Sales type | ✅ **Wholesale** — quantity discount tiers (global + per-product) |

### Quantity Discount Model

```
Admin can define:

GLOBAL TIERS (apply to all products unless overridden):
  ┌──────────┬──────────┬──────────────────┐
  │ Min Qty  │ Max Qty  │ Discount %       │
  ├──────────┼──────────┼──────────────────┤
  │   10     │   49     │     5%           │
  │   50     │   99     │    10%           │
  │   100    │  null    │    15%           │
  └──────────┴──────────┴──────────────────┘

PER-PRODUCT TIERS (override global for that specific product):
  Same structure, but linked to a ProductId.
  If a product has its own tiers → global tiers are ignored for it.
  If no product tiers → fall back to global tiers.
```

---

## Step-by-Step Task List

Tasks are ordered to be done sequentially. Each is atomic and completable in one session.
Mark `[ ]` → `[x]` as you complete them.

---

### 🏗 PHASE 1 — Solution Scaffold

- [x] **T-01** Create solution and projects
  ```bash
  dotnet new sln -n KitchenwareBot
  dotnet new classlib -n KitchenwareBot.Domain -o src/Domain
  dotnet new classlib -n KitchenwareBot.Application -o src/Application
  dotnet new classlib -n KitchenwareBot.Infrastructure -o src/Infrastructure
  dotnet new worker -n KitchenwareBot.Bot -o src/Bot
  dotnet new webapi -n KitchenwareBot.API -o src/API
  dotnet sln add src/Domain src/Application src/Infrastructure src/Bot src/API
  ```

- [x] **T-02** Add project references
  ```
  Application  → Domain
  Infrastructure → Application, Domain
  Bot          → Application, Infrastructure
  API          → Application, Infrastructure
  ```

- [x] **T-03** Add NuGet packages to each project
  ```xml
  <!-- Domain: no packages needed -->

  <!-- Application -->
  FluentValidation (MIT)

  <!-- Infrastructure -->
  Microsoft.EntityFrameworkCore.SqlServer
  Microsoft.EntityFrameworkCore.Tools
  StackExchange.Redis
  Microsoft.Extensions.Configuration.Abstractions

  <!-- Bot -->
  Telegram.Bot (21.x)
  Microsoft.Extensions.Hosting

  <!-- API -->
  Microsoft.AspNetCore.Authentication.JwtBearer
  Swashbuckle.AspNetCore
  ```

- [x] **T-04** Create `appsettings.json` and `.env` structure
  ```json
  {
    "ConnectionStrings": {
      "Default": "Server=(localdb)\\mssqllocaldb;Database=KitchenwareBot;Trusted_Connection=True;"
    },
    "Redis": { "Connection": "localhost:6379" },
    "Telegram": {
      "BotToken": "",
      "WebhookUrl": "",
      "BotUsername": "",
      "ChannelId": "",
      "AdminIds": []
    },
    "Jwt": {
      "SecretKey": "",
      "Issuer": "KitchenwareBot",
      "ExpiryMinutes": 60
    }
  }
  ```

---

### 🧱 PHASE 2 — Domain Layer

- [x] **T-05** Create enums
  ```
  OrderStatus:   Pending, Confirmed, Processing, Shipped, Delivered, Cancelled
  PaymentMethod: BankTransfer, Cash
  DeliveryType:  Shipping, InPerson
  UserRole:      Customer, Admin
  ```

- [x] **T-06** Create `Category` entity
  ```
  Id (Guid), Name (string), ParentId (Guid?), DisplayOrder (int), IsActive (bool)
  Navigation: Parent, Children, Products
  ```

- [x] **T-07** Create `Product` entity
  ```
  Id, Name, Description, Price (decimal), CategoryId,
  ImagePath (string?), TelegramFileId (string?),
  IsActive (bool), CreatedAt, UpdatedAt
  Navigation: Category, InventoryItems, OrderItems, DiscountTiers
  Methods: Create(), Update(), Activate(), Deactivate(), SetImage()
  ```

- [x] **T-08** Create `GlobalDiscountTier` entity
  ```
  Id (Guid), MinQuantity (int), MaxQuantity (int?),
  DiscountPercent (decimal), IsActive (bool), DisplayOrder (int)
  ```

- [x] **T-09** Create `ProductDiscountTier` entity
  ```
  Id (Guid), ProductId (Guid), MinQuantity (int), MaxQuantity (int?),
  DiscountPercent (decimal), IsActive (bool), DisplayOrder (int)
  Navigation: Product
  ```

- [x] **T-10** Create `Warehouse` entity
  ```
  Id, Name, Location (string?), IsActive (bool)
  Navigation: InventoryItems
  ```

- [x] **T-11** Create `InventoryItem` entity
  ```
  Id, ProductId, WarehouseId, Quantity (int), ReservedQuantity (int)
  LowStockThreshold (int, default 5)
  Computed: AvailableQuantity, IsLowStock
  Methods: Adjust(delta), Reserve(qty), Release(qty), Consume(qty)
  ```

- [x] **T-12** Create `AppUser` entity
  ```
  Id, TelegramId (long), Username (string?), FirstName (string?),
  Phone (string?), DefaultAddress (string?), Role (UserRole),
  IsBanned (bool), CreatedAt
  ```

- [x] **T-13** Create `Order` entity
  ```
  Id, CustomerTelegramId (long), CustomerName, CustomerPhone (string?),
  Status, PaymentMethod, DeliveryType, ShippingAddress (string?),
  AdminNote (string?), TotalAmount (decimal), CreatedAt, UpdatedAt
  Navigation: Items (List<OrderItem>)
  Methods: Create(), AddItem(), UpdateStatus(), RecalculateTotal()
  ```

- [x] **T-14** Create `OrderItem` entity
  ```
  Id, OrderId, ProductId, ProductName (snapshot), UnitPrice (snapshot),
  OriginalPrice (snapshot), DiscountPercent (decimal), Quantity (int)
  Computed: SubTotal
  ```

- [x] **T-15** Create `PaymentSettings` entity (singleton row)
  ```
  Id (Guid), BankTransferEnabled (bool), CashEnabled (bool),
  BankAccountName, BankAccountNumber, BankName, BankNote (string?)
  Computed: IsShopOpen => BankTransferEnabled || CashEnabled
  ```

- [x] **T-16** Create repository interfaces in Domain
  ```
  IProductRepository, IOrderRepository, IInventoryRepository,
  IUserRepository, IDiscountRepository, IPaymentSettingsRepository,
  IWarehouseRepository
  Each has: GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync + specifics
  ```

---

### 🗄 PHASE 3 — Infrastructure Layer (EF Core + SQL Server)

- [ ] **T-17** Create `AppDbContext` with all DbSets

- [ ] **T-18** Create EF Core configurations (IEntityTypeConfiguration<T>) for each entity
  - Precision for decimal columns (18,2)
  - Indexes on TelegramId, ProductId, etc.
  - Cascade delete rules
  - Seed: one default Warehouse, default PaymentSettings (both disabled)

- [ ] **T-19** Implement `IProductRepository`
  - `GetAllActiveAsync(categoryId?, page, pageSize)`
  - `GetByIdAsync(id)`
  - `SearchAsync(term)`
  - `GetWithInventoryAsync(id)`

- [ ] **T-20** Implement `IOrderRepository`
  - `GetByIdAsync(id)`
  - `GetByCustomerAsync(telegramId, page, pageSize)`
  - `GetAllAsync(status?, page, pageSize)` — for admin
  - `GetPendingCountAsync()`

- [ ] **T-21** Implement `IInventoryRepository`
  - `GetAvailableStockAsync(productId)`
  - `GetAllLowStockAsync()`
  - `GetWarehouseStockAsync(warehouseId)`
  - `ReserveAsync(productId, qty)`
  - `ReleaseAsync(productId, qty)`
  - `ConsumeAsync(productId, qty)`

- [ ] **T-22** Implement `IDiscountRepository`
  - `GetGlobalTiersAsync()` — ordered by MinQuantity
  - `GetProductTiersAsync(productId)`
  - `ResolveDiscountAsync(productId, quantity)` → returns `decimal discountPercent`
    Logic: if product has own tiers → use those; else use global tiers

- [ ] **T-23** Implement `IUserRepository`, `IPaymentSettingsRepository`, `IWarehouseRepository`

- [ ] **T-24** Implement `IUnitOfWork` and register all services in `DependencyInjection.cs`

- [ ] **T-25** Create `RedisBotStateService`
  ```csharp
  // Stores UserSession as JSON in Redis with 30-min TTL
  // Key: "bot:session:{telegramId}"
  Task<UserSession> GetOrCreateAsync(long telegramId)
  Task SetAsync(long telegramId, UserSession session)
  Task ClearAsync(long telegramId)
  ```

- [ ] **T-26** Create and run initial migration
  ```bash
  dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/Bot
  dotnet ef database update --project src/Infrastructure --startup-project src/Bot
  ```

---

### ⚙️ PHASE 4 — Application Layer (Services)

- [ ] **T-27** Create `ProductService`
  - `GetCategoriesAsync()`
  - `GetProductsAsync(categoryId, page, pageSize)`
  - `GetProductDetailAsync(id)` → includes discount tiers + stock
  - `CreateProductAsync(dto)`
  - `UpdateProductAsync(id, dto)`
  - `DeleteProductAsync(id)` — soft delete (IsActive = false)
  - `SetProductImageAsync(id, imagePath, telegramFileId)`

- [ ] **T-28** Create `OrderService`
  - `CalculateOrderAsync(cart, telegramId)` → returns itemized totals with discounts applied
  - `PlaceOrderAsync(telegramId, customerName, cart, draft)` → validates stock, reserves, creates order, notifies admins
  - `GetCustomerOrdersAsync(telegramId, page)`
  - `GetAllOrdersAsync(status?, page)` — admin
  - `UpdateOrderStatusAsync(orderId, status, note?)`
  - `CancelOrderAsync(orderId)` → releases stock reservation

- [ ] **T-29** Create `InventoryService`
  - `GetStockReportAsync()`
  - `GetLowStockAlertsAsync()`
  - `AdjustStockAsync(productId, warehouseId, delta, reason)`
  - `GetWarehousesAsync()`

- [ ] **T-30** Create `DiscountService`
  - `GetGlobalTiersAsync()`
  - `AddGlobalTierAsync(dto)`
  - `UpdateGlobalTierAsync(id, dto)`
  - `DeleteGlobalTierAsync(id)`
  - `GetProductTiersAsync(productId)`
  - `AddProductTierAsync(productId, dto)`
  - `UpdateProductTierAsync(id, dto)`
  - `DeleteProductTierAsync(id)`
  - `ResolveDiscountAsync(productId, quantity)` → decimal percent

- [ ] **T-31** Create `PaymentSettingsService`
  - `GetAsync()`
  - `UpdateAsync(dto)` → toggle bank/cash, update bank details

- [ ] **T-32** Create `UserService`
  - `GetOrCreateAsync(telegramId, username, firstName)`
  - `IsAdminAsync(telegramId)` — checks DB role OR config AdminIds list
  - `BanAsync(telegramId)` / `UnbanAsync(telegramId)`

- [ ] **T-33** Create `PriceFormatter` static helper
  ```csharp
  // Persian digits + Toman suffix
  // 1500000 → "۱٬۵۰۰٬۰۰۰ تومان"
  public static string FormatToman(decimal amount)
  public static string FormatPercent(decimal percent)  // "۱۵٪"
  ```

- [ ] **T-34** Create `BotMessages` static class — all Persian string constants
  ```csharp
  // All user-facing messages in one file for easy editing
  public static class BotMessages
  {
      public const string Welcome = "سلام {0} عزیز! 👋\nبه فروشگاه لوازم آشپزخانه خوش آمدید.";
      public const string MainMenu = "از منوی زیر انتخاب کنید:";
      public const string ShopClosed = "🔒 فروشگاه در حال حاضر تعطیل می‌باشد.";
      public const string OutOfStock = "❌ موجودی این محصول کافی نیست.";
      // ... all messages
  }
  ```

---

### 🤖 PHASE 5 — Bot Layer (Core Infrastructure)

- [ ] **T-35** Define `BotState` enum (all FSM states)
  ```csharp
  // Customer
  Idle, MainMenu,
  BrowsingCategories, BrowsingProducts, ViewingProduct,
  Cart,
  CheckoutAskDelivery, CheckoutAskAddress, CheckoutAskPayment, CheckoutConfirm,
  MyOrders, ViewingOrder,

  // Admin
  AdminMenu,
  // Products
  AdminProductList, AdminProductAskName, AdminProductAskDescription,
  AdminProductAskPrice, AdminProductAskCategory, AdminProductAskStock,
  AdminProductAskImage, AdminProductPreview,
  // Orders
  AdminOrderList, AdminViewingOrder, AdminOrderAskNote,
  // Inventory
  AdminInventoryMenu, AdminAdjustStockAskProduct, AdminAdjustStockAskQty,
  // Discounts
  AdminDiscountMenu, AdminGlobalDiscountList,
  AdminGlobalDiscountAskMin, AdminGlobalDiscountAskMax, AdminGlobalDiscountAskPercent,
  AdminProductDiscountList, AdminProductDiscountAskMin, AdminProductDiscountAskMax, AdminProductDiscountAskPercent,
  // Settings
  AdminSettings, AdminPaymentSettings, AdminBankDetails
  ```

- [ ] **T-36** Define `UserSession` class (serialized to Redis)
  ```csharp
  public class UserSession
  {
      public long TelegramId { get; set; }
      public BotState State { get; set; } = BotState.Idle;
      public List<CartItem> Cart { get; set; } = new();
      public OrderDraft? OrderDraft { get; set; }
      public ProductDraft? ProductDraft { get; set; }   // admin creating product
      public DiscountDraft? DiscountDraft { get; set; } // admin creating tier
      public Guid? SelectedProductId { get; set; }      // context for admin actions
      public Guid? SelectedCategoryId { get; set; }
      public int CurrentPage { get; set; } = 1;
  }

  public class CartItem { Guid ProductId; string Name; decimal UnitPrice; int Qty; }
  public class OrderDraft { PaymentMethod Payment; DeliveryType Delivery; string? Address; }
  public class ProductDraft { string? Name; string? Desc; decimal? Price; Guid? CategoryId; int? Stock; }
  public class DiscountDraft { int? MinQty; int? MaxQty; decimal? Percent; Guid? ProductId; }
  ```

- [ ] **T-37** Create `CustomerKeyboards` static class
  ```
  MainMenuKeyboard()          → ReplyKeyboardMarkup with main options
  CategoryListKeyboard(list)  → InlineKeyboardMarkup
  ProductListKeyboard(products, page, totalPages) → with ◄ ► pagination
  ProductDetailKeyboard(productId) → qty buttons: ۱ ۲ ۵ ۱۰ ۲۰ سفارشی
  CartKeyboard()              → تسویه‌حساب | ویرایش | پاک کردن
  DeliveryKeyboard()          → ارسال پستی | تحویل حضوری
  PaymentKeyboard(settings)   → only shows enabled methods
  ConfirmOrderKeyboard()      → تأیید سفارش | انصراف
  OrderListKeyboard(orders, page)
  ```

- [ ] **T-38** Create `AdminKeyboards` static class
  ```
  AdminMainKeyboard()
  AdminProductListKeyboard(products, page)
  AdminProductActionKeyboard(productId) → ویرایش | حذف | انتشار در کانال | تنظیم تخفیف
  AdminOrderListKeyboard(orders, page, statusFilter)
  AdminOrderActionKeyboard(orderId, currentStatus)
  AdminInventoryKeyboard()
  AdminDiscountMenuKeyboard()
  AdminGlobalDiscountListKeyboard(tiers)
  AdminProductDiscountListKeyboard(productId, tiers)
  AdminSettingsKeyboard()
  AdminPaymentSettingsKeyboard(settings)
  ```

- [ ] **T-39** Create `UpdateRouter` — routes incoming Update to correct handler based on state

- [ ] **T-40** Register Telegram webhook in `Program.cs` and map `/telegram/webhook` endpoint

---

### 👤 PHASE 6 — Customer Handlers

- [ ] **T-41** `StartHandler` — handle `/start`, deeplink parsing (`start=product_X`, `start=buy_X_Y`)
  - Get/create user in DB
  - If deeplink → route to product or add-to-cart
  - Else → show main menu

- [ ] **T-42** `CatalogHandler` — category list → product list (paginated) → product detail
  - Show product: name, description, price, **discount tier table**, stock indicator
  - Discount table example:
    ```
    📊 جدول تخفیف:
    ۱۰ تا ۴۹ عدد: ۵٪ تخفیف
    ۵۰ تا ۹۹ عدد: ۱۰٪ تخفیف
    ۱۰۰ عدد و بیشتر: ۱۵٪ تخفیف
    ```

- [ ] **T-43** `CartHandler`
  - Add item (from product detail qty selection)
  - Show cart summary with per-item discount applied, running total
  - Edit quantity of existing item
  - Remove item
  - Clear cart
  - Live recalculation as items added

- [ ] **T-44** `CheckoutHandler`
  - Step 1: Ask delivery type
  - Step 2 (if shipping): Ask address (free text)
  - Step 3: Ask payment method (only show enabled options)
  - Step 4: Show final summary — itemized with discounts, grand total
  - Step 5: Confirm → call `OrderService.PlaceOrderAsync()`
  - On success: show order number + payment instructions if bank transfer
  - On stock fail: show which item ran out

- [ ] **T-45** `MyOrdersHandler`
  - List customer's orders (paginated), newest first
  - Show order detail: items, amounts, status, date
  - Persian status labels: در انتظار تأیید | تأیید شده | در حال پردازش | ارسال شده | تحویل داده شده | لغو شده

---

### 🔐 PHASE 7 — Admin Handlers

- [ ] **T-46** `AdminMenuHandler` — entry point, check admin role, show admin main menu

- [ ] **T-47** `ProductAdminHandler` — full product CRUD
  - List products (with page navigation)
  - Add: step through Name → Description → Price → Category → Stock → Image (optional, can skip)
  - Edit: show current value, accept new value
  - Delete: confirm step
  - Toggle active/inactive
  - After save: ask "انتشار در کانال؟" → if yes → call publish

- [ ] **T-48** `ChannelPublishHandler`
  - Build channel message: photo + Persian caption + discount table
  - Attach inline buttons with deeplinks:
    ```
    [🛒 سفارش دهید]
    [📦 ۱ عدد] [📦 ۵ عدد] [📦 ۱۰ عدد]
    [🔢 تعداد دلخواه]
    ```
  - Send to configured channel

- [ ] **T-49** `OrderAdminHandler`
  - List all orders, filterable by status
  - View order detail (customer info, items, totals, payment method)
  - Update status with optional note
  - When status updated → send Telegram notification to customer
  - Customer notification examples:
    ```
    ✅ سفارش شما تأیید شد.
    🚚 سفارش شما ارسال شد. کد پیگیری: [note]
    ❌ سفارش شما لغو شد. دلیل: [note]
    ```

- [ ] **T-50** `InventoryAdminHandler`
  - View stock report (all products, available qty)
  - View low-stock alerts (products below threshold)
  - Adjust stock: pick product → pick warehouse → enter delta (+/-) → confirm
  - View warehouses list

- [ ] **T-51** `DiscountAdminHandler`
  - **Global tiers**: list → add → edit → delete → reorder
  - **Per-product tiers**:
    - From product detail page → "تنظیم تخفیف"
    - Shows product's own tiers (or global if none)
    - Add/edit/delete tiers for this product
    - Button to "حذف تخفیف اختصاصی و استفاده از تخفیف عمومی"

- [ ] **T-52** `SettingsAdminHandler`
  - Payment settings:
    - Toggle bank transfer on/off
    - Toggle cash on/off
    - Edit bank name, account number, account holder name, note
    - Show current status: "وضعیت فروشگاه: باز / بسته"
  - Channel settings: set/update channel ID

---

### 🧪 PHASE 8 — Testing & Hardening

- [ ] **T-53** Manual test: Customer full flow
  - Browse → find product → view discount table → add to cart (qty 15) → verify discount applied → checkout → confirm

- [ ] **T-54** Manual test: Admin product CRUD
  - Add product with image → publish to channel → verify buttons work → edit price → verify channel message outdated (expected)

- [ ] **T-55** Manual test: Discount tiers
  - Set global tiers → order product at various quantities → verify correct discount
  - Set product-specific tiers → verify they override global
  - Delete product tiers → verify fallback to global

- [ ] **T-56** Manual test: Inventory flow
  - Set stock to 10 → two customers try to order 6 each → first succeeds, second gets "موجودی کافی نیست"
  - Admin confirms first order → stock consumed → second customer tries again → still fails

- [ ] **T-57** Manual test: Payment settings
  - Disable both methods → customers see shop closed message
  - Enable only cash → bank transfer option hidden in checkout
  - Enable both → both options shown

- [ ] **T-58** Error handling hardening
  - Invalid text input during numeric steps (price, qty, stock)
  - Session timeout (30 min) → graceful restart
  - Telegram API rate limiting (add retry with exponential backoff)
  - DB connection failure messages

---

### 🐳 PHASE 9 — Docker & Deployment

- [ ] **T-59** Write `Dockerfile` for the Bot project (multi-stage build)

- [ ] **T-60** Write `docker-compose.yml`
  ```
  Services: app (bot+api), sqlserver, redis
  Volumes: sqldata, redisdata
  ```

- [ ] **T-61** Configure Nginx as reverse proxy with SSL (or use Traefik)
  - HTTPS required for Telegram webhook
  - Route `/telegram/webhook` → app

- [ ] **T-62** Create deployment script or GitHub Actions workflow
  - Build image → push to registry → SSH to server → pull → `docker compose up -d`
  - Run EF migrations on deploy

- [ ] **T-63** Register webhook after deployment
  ```bash
  curl "https://api.telegram.org/bot{TOKEN}/setWebhook?url=https://yourdomain.com/telegram/webhook"
  ```

---

### 🌐 PHASE 10 — REST API (Website Integration, Future)

> Do this phase when you're ready to build the website. The Application layer services are already built — this is just wiring them to HTTP endpoints.

- [ ] **T-64** Scaffold `ProductsController` — public endpoints (no auth) for catalog
- [ ] **T-65** Scaffold `OrdersController` — customer-authenticated order placement and history
- [ ] **T-66** Scaffold `AdminController` — admin-authenticated full management
- [ ] **T-67** Implement JWT auth — `/api/auth/login` with Telegram ID + secret or username/password
- [ ] **T-68** Add account linking — customer can link their Telegram account to their website account
  - When website order placed → if linked → bot sends confirmation to Telegram
- [ ] **T-69** Add Swagger/OpenAPI docs
- [ ] **T-70** CORS configuration for website domain

---

## Summary

| Phase | Tasks | Who/What |
|-------|-------|----------|
| 1 — Scaffold | T-01 to T-04 | Claude Code (terminal) |
| 2 — Domain | T-05 to T-16 | Claude Code (code gen) |
| 3 — Infrastructure | T-17 to T-26 | Claude Code (code gen) |
| 4 — Application | T-27 to T-34 | Claude Code (code gen) |
| 5 — Bot Core | T-35 to T-40 | Claude Code (code gen) |
| 6 — Customer UX | T-41 to T-45 | Claude Code + manual review |
| 7 — Admin UX | T-46 to T-52 | Claude Code + manual review |
| 8 — Testing | T-53 to T-58 | You (manual testing) |
| 9 — DevOps | T-59 to T-63 | You / Claude Code |
| 10 — API | T-64 to T-70 | Future phase |

**Total: 70 tasks across 10 phases**
Estimated development time with Claude Code assistance: **3–5 weeks** (working part-time)
