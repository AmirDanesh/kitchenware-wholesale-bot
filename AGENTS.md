# KitchenwareBot — Project Context

Wholesale kitchenware Telegram bot with REST API for future website integration.
Persian-only UI, Toman pricing, webhook mode, SQL Server, Redis FSM in Release.

> Full details imported below. Keep this file under 200 lines.

@docs/BUSINESS_RULES.md
@docs/ARCHITECTURE.md
@docs/TASKS.md

---

## Solution Structure

```
src/
├── KitchenwareBot.Domain/         ← Entities, enums, interfaces. Zero dependencies.
├── KitchenwareBot.Application/    ← All business logic. Service classes. No HTTP, no Telegram.
├── KitchenwareBot.Infrastructure/ ← EF Core + SQL Server, Redis, repositories.
├── KitchenwareBot.Bot/            ← Telegram handlers, FSM router, keyboards. Thin layer.
└── KitchenwareBot.API/            ← REST API for website (Phase 10, scaffold only for now).
```

Dependency flow (one direction only):
```
Bot ──► Application ◄── API
Infrastructure ──► Application
Domain ◄── all projects
```

---

## Tech Stack

| Concern | Choice |
|---------|--------|
| Runtime | .NET 8, C# |
| Bot | Telegram.Bot 21.x — **Webhook** in production, **Polling** in dev |
| Database | EF Core InMemory (Debug), SQL Server (Release/prod) |
| ORM | EF Core 8, `Microsoft.EntityFrameworkCore.SqlServer` |
| State | `IMemoryCache` (Debug), Redis via `StackExchange.Redis` (Release) |
| Validation | FluentValidation |
| Mediator | ❌ None — plain service classes injected via DI |
| Containerization | Docker + docker-compose |

---

## Non-Negotiable Rules — Apply in Every File

1. **All bot-facing text is Persian.** Never show English to users. All strings in `BotMessages.cs`.
2. **All prices use `PriceFormatter.FormatToman(decimal)`** → output: `۱٬۵۰۰٬۰۰۰ تومان`
3. **Business logic belongs in `Application/` only.** Bot and API are thin entry points.
4. **No MediatR.** Services are injected directly into handlers.
5. **No static state.** Everything flows through DI.
6. **Naming**: PascalCase classes, camelCase locals, `I` prefix for interfaces, `Async` suffix for async methods.
7. **Migrations**: always code-first. Never edit DB manually.

---

## Key Patterns at a Glance

**Bot FSM (Finite State Machine)**
- Each user has a JSON-serialized `UserSession`: `IMemoryCache` in Debug, Redis in Release
- Key: `bot:session:{telegramId}`, TTL: 30 minutes
- `UpdateRouter` reads session state → dispatches to correct handler
- Handlers update state + save session before returning

**Service Layer**
Services live in `Application/Services/`:
`ProductService`, `OrderService`, `InventoryService`,
`DiscountService`, `PaymentSettingsService`, `UserService`

**Discount Resolution** (in `DiscountService.ResolveDiscountAsync`)
```
if product has own DiscountTiers → use those
else use GlobalDiscountTiers
→ find tier where MinQty <= qty <= MaxQty
→ return DiscountPercent (0 if no match)
```

**Inventory Flow**
```
Order placed   → stock.Reserve(qty)
Order confirmed → stock.Consume(qty)   [Reserve + actual deduction]
Order cancelled → stock.Release(qty)
```

---

## How to Run Locally

```bash
# 1. Debug uses in-memory database and session cache; SQL Server and Redis are not needed

# Apply SQL Server migrations for Release/relational testing when needed
dotnet ef database update \
  --project src/Infrastructure \
  --startup-project src/Bot

# 2. Run Debug in polling mode (leave WebhookUrl empty in appsettings.Development.json)
dotnet run --project src/Bot

# 3. New migration
dotnet ef migrations add MigrationName \
  --project src/Infrastructure \
  --startup-project src/Bot
```

---

## Current Status

See `docs/TASKS.md` for the full task list with checkboxes.
Update checkboxes as tasks complete so this project state stays current.
