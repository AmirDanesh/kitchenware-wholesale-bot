# KitchenwareBot 🍳

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com)
[![Telegram Bot API](https://img.shields.io/badge/Telegram%20Bot-API%2021.x-0088cc)](https://core.telegram.org/bots/api)

A production-grade **wholesale kitchenware shop bot** for Telegram with inventory management, quantity-based discounts, and REST API integration. Built with .NET 8, SQL Server, and Redis.

**Language:** Persian (Farsi) · **Currency:** Iranian Toman (IRT) · **Target:** B2B wholesale retailers and resellers

---

## ✨ Features

### 👥 Customer Experience
- 🔍 **Browse catalog** by categories with pagination
- 🛒 **Smart cart** with real-time discount calculation
- 💰 **Quantity discounts** — automatic wholesale pricing tiers
- 📋 **Checkout flow** — delivery & payment method selection
- 📱 **Order tracking** — status updates via Telegram
- 🔗 **Deep linking** — direct product/cart links via Telegram

### 🛡️ Admin Dashboard
- ➕ **Product management** — CRUD with image uploads
- 📊 **Inventory tracking** — multi-warehouse stock management
- 📈 **Discount tiers** — global + per-product quantity breaks
- 💳 **Payment settings** — bank transfer & cash configuration
- 📢 **Channel publishing** — auto-post products to Telegram channel
- 📧 **Order notifications** — instant admin alerts for new orders

### 🏗️ Technical
- ⚡ **Webhook mode** — instant Telegram updates (production)
- 🗄️ **SQL Server** — robust data persistence
- 🔴 **Redis FSM** — efficient conversation state management
- 🌐 **REST API** — ready for website integration (Phase 10)
- 🐳 **Docker ready** — containerized deployment

---

## 🚀 Quick Start

### Prerequisites
- **.NET 8 SDK**
- **SQL Server** (LocalDB for dev, Express for prod)
- **Redis** (local or Docker)
- **Telegram Bot Token** (from [@BotFather](https://t.me/botfather))

### Local Development

```bash
# 1. Clone and navigate
git clone https://github.com/yourusername/kitchenware-wholesale-bot.git
cd kitchenware-wholesale-bot

# 2. Start Redis
docker run -d -p 6379:6379 redis:alpine

# 3. Configure secrets (Development only)
# Edit src/Bot/appsettings.Development.json:
{
  "Telegram": {
    "BotToken": "your_bot_token_here",
    "BotUsername": "your_bot_username",
    "AdminIds": [123456789],
    "ChannelId": -1001234567890
  }
}

# 4. Initialize database (LocalDB auto-starts)
dotnet ef database update \
  --project src/KitchenwareBot.Infrastructure \
  --startup-project src/KitchenwareBot.Bot

# 5. Run bot in polling mode
dotnet run --project src/KitchenwareBot.Bot
```

### Docker Deployment

```bash
docker-compose up -d
```

See [Docker Deployment Guide](./docs/DEPLOYMENT.md) for production setup.

---

## 📁 Project Structure

```
src/
├── KitchenwareBot.Domain/        # Entities, enums, interfaces (0 dependencies)
├── KitchenwareBot.Application/   # Business logic & services (Domain only)
├── KitchenwareBot.Infrastructure # EF Core, SQL Server, Redis, repositories
├── KitchenwareBot.Bot/           # Telegram handlers, FSM router, keyboards
└── KitchenwareBot.API/           # REST API (scaffold for Phase 10)

docs/
├── ARCHITECTURE.md               # Tech decisions & patterns
├── BUSINESS_RULES.md             # Requirements & workflows
└── TASKS.md                       # Implementation roadmap (70 tasks)
```

**Dependency flow (strict one-way):**
```
Bot ──► Application ◄── API
Infrastructure ──► Application
Domain ◄── all projects
```

---

## 💻 Tech Stack

| Layer | Technology |
|-------|-----------|
| **Runtime** | .NET 8, C# |
| **Bot Framework** | Telegram.Bot 21.x (Webhook + Polling) |
| **Database** | SQL Server with EF Core 8 |
| **State Management** | Redis (JSON-serialized FSM) |
| **Validation** | FluentValidation |
| **Architecture** | Clean Architecture, Service Layer pattern |
| **Containerization** | Docker + docker-compose |

---

## 📚 Documentation

- **[Architecture Decisions](./docs/ARCHITECTURE.md)** — Why these choices? Clean architecture patterns, Redis FSM design
- **[Business Rules](./docs/BUSINESS_RULES.md)** — Complete business logic, workflows, order/discount/inventory rules
- **[Implementation Roadmap](./docs/TASKS.md)** — 70 tasks across 10 phases; track progress with checkboxes
- **[Deployment Guide](./docs/DEPLOYMENT.md)** — Docker, Nginx, SSL, webhook registration (coming soon)

---

## 🎯 Key Patterns

### Quantity Discount Resolution
```csharp
if product.DiscountTiers.Any() → use product-specific tiers
else → use global tiers
→ find tier where MinQty ≤ qty ≤ MaxQty
→ return DiscountPercent (0 if no match)
```

### Bot FSM State Management
```
UserSession stored in Redis
  ├─ Key: bot:session:{telegramId}
  ├─ TTL: 30 minutes
  └─ State enum + Cart + Drafts
```

### Inventory Lifecycle
```
Place order  → Reserve(qty)
Confirm      → Consume(qty) [Reserve + actual deduction]
Cancel       → Release(qty) [Restore available]
```

---

## 🔒 Security & Secrets

### Never Commit
- `appsettings.Development.json` — contains `BotToken`, `AdminIds`
- `appsettings.Production.json` — production database connection string
- `.env` files — any local configuration

See [.gitignore](./.gitignore) for excluded patterns.

### Production Checklist
- ✅ Use environment variables or Azure Key Vault for secrets
- ✅ Enable HTTPS on webhook endpoint
- ✅ Validate Telegram update signatures
- ✅ Set up database backups
- ✅ Monitor Redis memory usage
- ✅ Enable SQL Server encryption

---

## 🛠️ Development Workflow

### Adding a Feature
1. Create a branch: `git checkout -b feature/my-feature`
2. Follow [CONTRIBUTING.md](./CONTRIBUTING.md)
3. Submit a Pull Request with:
   - Concise description of changes
   - Link to related issue (if any)
   - Test evidence (screenshots for UI, test output for logic)

### Running Tests
```bash
# Unit tests (when added)
dotnet test
```

### Database Migrations
```bash
# Create a migration
dotnet ef migrations add DescriptiveName \
  --project src/KitchenwareBot.Infrastructure \
  --startup-project src/KitchenwareBot.Bot

# Apply locally
dotnet ef database update \
  --project src/KitchenwareBot.Infrastructure \
  --startup-project src/KitchenwareBot.Bot
```

---

## 📈 Implementation Status

**Phase Progress:** See [TASKS.md](./docs/TASKS.md) for detailed checklist.

- ✅ Phase 1 — Solution scaffold
- ✅ Phase 2 — Domain layer
- ✅ Phase 3 — Infrastructure (in progress)
- ⏳ Phase 4 — Application services
- ⏳ Phase 5 — Bot core FSM
- ⏳ Phase 6–7 — Customer & Admin handlers
- ⏳ Phase 8–9 — Testing & deployment
- 📅 Phase 10 — REST API for website

---

## 🤝 Contributing

Contributions welcome! Please read [CONTRIBUTING.md](./CONTRIBUTING.md) first.

**To report a bug or suggest a feature:**
1. Check [Issues](../../issues) to avoid duplicates
2. Create a new issue with the appropriate template
3. Provide clear description + reproduction steps

---

## 📄 License

This project is licensed under the **MIT License** — see [LICENSE](./LICENSE) file for details.

You are free to:
- ✅ Use in commercial projects
- ✅ Modify and distribute
- ✅ Use privately

You must include the license and copyright notice.

---

## 👨‍💻 Author

Built with ❤️ by Amir Daneshvar & Claude

---

## 🙏 Acknowledgments

- [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) — Official Telegram Bot API wrapper
- [Entity Framework Core](https://github.com/dotnet/efcore) — Data access
- [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) — Redis client

---

## 📞 Support

- 📖 **[Full Documentation](./docs/)**
- 🐛 **[Report Issues](../../issues)**
- 💬 **[Discussions](../../discussions)**
- 📧 **Email:** amir77daneshvar@gmail.com

---

**Made in 🇮🇷 Iran** | *Wholesale at scale. Toman proud.*
