# Architecture Decisions

## Why These Choices Were Made

### No MediatR
MediatR 12+ requires a commercial license. Replaced with plain service classes
injected via DI. The result is simpler, equally testable, and has zero cost.
Do NOT introduce MediatR. If you need to decouple something, use an interface.

### SQL Server over PostgreSQL
The developer is a .NET specialist. SQL Server integrates natively with Visual Studio,
supports LocalDB for zero-config dev, and SQL Server Express is free for production
at this scale. No Npgsql knowledge required.

### No Domain Events Library
Domain events (like `OrderPlaced`) are handled by direct service calls for now.
Example: `OrderService.PlaceOrderAsync()` directly calls `_notifier.NotifyAdminsAsync()`.
This is intentional — keep it simple until the system grows.

### Webhook over Polling (Production)
- Webhook: Telegram pushes updates instantly → faster, more efficient
- The app switches automatically: if `Telegram:WebhookUrl` is empty → polling mode (dev)
- If `Telegram:WebhookUrl` is set → webhook mode (production)

### Redis for Bot State (FSM)
Each user's conversation state (which step of the flow they're in, their cart contents,
admin product draft in progress) is stored in Redis as a JSON-serialized `UserSession`.
- Key: `bot:session:{telegramId}`
- TTL: 30 minutes (session expires if user is idle)
- Why not DB: session data is ephemeral, high-frequency read/write, not business data

### Service Layer is the Integration Point
When the website is built (Phase 10), the REST API project will inject and call
the SAME service classes (`ProductService`, `OrderService`, etc.) that the bot uses.
No code duplication. No divergence. The bot and website share one source of truth.

---

## Project Dependency Rules

```
Domain          → no dependencies
Application     → Domain only
Infrastructure  → Application + Domain
Bot             → Application + Infrastructure
API             → Application + Infrastructure
```

**Violations to refuse:**
- Domain importing from any other layer → ❌
- Application importing from Bot or API → ❌
- Infrastructure importing from Bot or API → ❌
- Bot calling DB directly (bypass Application) → ❌

---

## EF Core Conventions

- All entities have `Id` as `Guid` (not int — safer for future distributed systems)
- Decimal columns: precision 18, scale 2 — set in EF config for all `decimal` properties
- Soft deletes via `IsActive` flag — never `DELETE` from Products
- Indexes: `AppUsers.TelegramId`, `Orders.CustomerTelegramId`, `Orders.Status`
- `CreatedAt` and `UpdatedAt` auto-set via `SaveChanges` override in `AppDbContext`
- All entity configs in `Infrastructure/Persistence/Configurations/` as separate files

---

## Redis Key Convention

```
bot:session:{telegramId}        ← UserSession FSM state, TTL 30min
```
Add new keys here when introducing new Redis usage.

---

## API Authentication (Phase 10)

JWT Bearer tokens. The API will have two token types:
- **Customer token**: issued after linking Telegram account, scoped to own orders
- **Admin token**: full access, issued to admin users

The Telegram bot itself does NOT use JWT — it trusts Telegram's update objects
and validates admin access via `UserService.IsAdminAsync(telegramId)`.

---

## File Naming Conventions

```
Entities:           Product.cs, Order.cs (singular)
Services:           ProductService.cs, OrderService.cs
Repositories:       ProductRepository.cs (implements IProductRepository)
Interfaces:         IProductRepository.cs, IProductService.cs
Bot handlers:       CatalogHandler.cs, CheckoutHandler.cs
Keyboards:          CustomerKeyboards.cs, AdminKeyboards.cs
Configurations:     ProductConfiguration.cs (EF IEntityTypeConfiguration)
DTOs:               CreateProductDto.cs, ProductDetailDto.cs
```

---

## Error Handling Strategy

- **Bot handlers**: wrap in try/catch, send Persian error message to user on exception
- **Services**: throw typed exceptions (`InsufficientStockException`, `ShopClosedException`)
- **Infrastructure**: let EF/Redis exceptions bubble up — handled at handler level
- **Never show stack traces or English error messages to Telegram users**

Standard error messages (in `BotMessages.cs`):
```csharp
public const string GenericError = "❌ خطایی رخ داد. لطفاً دوباره تلاش کنید.";
public const string ShopClosed   = "🔒 فروشگاه در حال حاضر تعطیل می‌باشد.";
public const string OutOfStock   = "❌ موجودی کافی نیست.";
public const string Unauthorized = "⛔ شما دسترسی لازم را ندارید.";
```

---

## What NOT to Do

- ❌ Do not add new NuGet packages without checking license (commercial use)
- ❌ Do not put Persian strings inline in handlers — use `BotMessages.cs`
- ❌ Do not format prices manually — always use `PriceFormatter.FormatToman()`
- ❌ Do not call Telegram Bot API from `Application/` layer — only from `Bot/` layer
- ❌ Do not create a DbContext in Bot or API projects — only Infrastructure owns it
- ❌ Do not use `int` for entity IDs — use `Guid`
