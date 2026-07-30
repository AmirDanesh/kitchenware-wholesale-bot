# KitchenwareBot

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Telegram.Bot](https://img.shields.io/badge/Telegram.Bot-22.10.2-26A5E4)](https://github.com/TelegramBots/Telegram.Bot)

Persian Telegram bot for wholesale kitchenware sales. KitchenwareBot manages the product catalog, quantity-based discounts, cart and checkout, orders, inventory, payment settings, and Telegram channel publishing.

- **Audience:** retailers, resellers, and bulk buyers
- **Language:** Persian for all bot-facing text
- **Currency:** Iranian Toman with Persian digits
- **Runtime:** polling during local development, webhook in production
- **Website integration:** REST API project is scaffolded for a future phase

## Features

### Customer

- Browse categories and paginated product lists
- View product details, stock, and discount tiers
- Add fixed or custom quantities to a cached cart
- Choose shipping or in-person delivery
- Choose any payment method currently enabled by an administrator
- Place orders with atomic stock reservation and locked price snapshots
- Track paginated order history and status changes
- Open product and purchase deep links from Telegram channel posts

### Administrator

- Manage products, categories, images, and active status
- Publish products to a Telegram channel
- Review and update orders with customer notifications
- View stock reports, low-stock items, and adjust warehouse inventory
- Manage global and product-specific quantity discounts
- Configure bank transfer, cash payment, bank details, and channel ID
- Grant access through configured Telegram IDs or database roles

### Platform

- SQL Server with EF Core 8 code-first migrations
- In-process conversation state in Debug; Redis-backed state in Release
- Plain application services through dependency injection; no MediatR
- Automatic polling/webhook mode selection
- Optional webhook secret-token validation
- Docker Compose stack with bot, SQL Server 2022, and Redis 7.4
- Health endpoint at `/health`

## Architecture

```text
src/
├── Domain/          Entities, enums, exceptions, repository contracts
├── Application/     Business services, DTOs, validation, messages, sessions
├── Infrastructure/  EF Core, repositories, in-memory/Redis state
├── Bot/             Telegram routing, handlers, keyboards, hosting
└── API/             Future website REST API scaffold
```

Dependencies point inward:

```text
Domain
  └── Application
        ├── Infrastructure
        ├── Bot
        └── API
```

Business rules stay in `Application`. `Bot` and `API` remain thin entry points. See [Architecture Decisions](docs/ARCHITECTURE.md) for constraints and design rationale.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server for Release builds, migrations, and database-sensitive testing
- Redis for Release builds and Docker deployment
- Telegram bot token from [@BotFather](https://t.me/BotFather)
- Docker Desktop or Docker Engine with Compose for container deployment

## Local Development

### 1. Clone

```powershell
git clone https://github.com/AmirDanesh/kitchenware-wholesale-bot.git
cd kitchenware-wholesale-bot
```

### 2. Configure development secrets

The Bot project has a .NET user-secrets ID. Store local credentials outside tracked configuration:

```powershell
dotnet user-secrets set "Telegram:BotToken" "YOUR_BOT_TOKEN" --project src/Bot
dotnet user-secrets set "Telegram:BotUsername" "YOUR_BOT_USERNAME" --project src/Bot
dotnet user-secrets set "Telegram:AdminIds:0" "YOUR_TELEGRAM_ID" --project src/Bot
```

Debug builds use non-persistent EF Core InMemory and `IMemoryCache`; SQL Server and Redis are not required. All data and bot sessions reset whenever the process stops. Release builds use SQL Server and Redis.

### 3. Apply database migrations when using SQL Server

```powershell
dotnet ef database update --project src/Infrastructure --startup-project src/Bot
```

Release builds also apply pending EF Core migrations during startup. Debug builds initialize the InMemory database instead.

### 4. Run

```powershell
dotnet run --project src/Bot
```

Leave `Telegram:WebhookUrl` empty for polling mode. With a valid URL configured, the hosted service registers a webhook and receives updates at `POST /telegram/webhook`.

## Docker Deployment

Create production configuration from the provided template, fill every required secret, then start the stack:

```powershell
Copy-Item .env.example .env
docker compose up --detach --build
docker compose ps
docker compose logs --follow bot
```

The Compose stack exposes the bot on port `8080` by default and keeps SQL Server and Redis on an internal network. Set `BOT_HTTP_PORT` to change the host port.

Production webhook configuration requires:

- Public HTTPS URL ending in `/telegram/webhook`
- `Telegram__WebhookUrl` set to that URL
- Optional matching `Telegram__WebhookSecretToken`
- Reverse proxy and TLS configuration

See [Deployment Guide](docs/DEPLOYMENT.md) for full server setup.

## Common Commands

```powershell
# Build solution
dotnet build KitchenwareBot.sln

# Run tests when test projects are added
dotnet test KitchenwareBot.sln

# Add migration
dotnet ef migrations add MigrationName --project src/Infrastructure --startup-project src/Bot

# Apply migration
dotnet ef database update --project src/Infrastructure --startup-project src/Bot

# Check container health
Invoke-WebRequest http://localhost:8080/health
```

## Discount and Inventory Rules

Product discount tiers completely replace global tiers for that product. If no product tiers exist, global tiers apply. The first active tier containing the requested quantity supplies the discount; otherwise the discount is zero.

```text
Order placed:    reserve stock
Order confirmed: consume reservation and physical stock
Order cancelled: release or restock inventory, depending on order status
```

Order items store original price, discount percentage, final unit price, and product name snapshots. Later product changes never alter existing orders.

## Configuration

| Key | Purpose |
|---|---|
| `ConnectionStrings:Default` | SQL Server connection string |
| `Redis:Connection` | Redis endpoint |
| `Redis:SessionTtlMinutes` | Conversation-state lifetime |
| `Telegram:BotToken` | Telegram bot credential |
| `Telegram:WebhookUrl` | Enables webhook mode when non-empty |
| `Telegram:WebhookSecretToken` | Protects webhook deliveries |
| `Telegram:BotUsername` | Builds channel deep links |
| `Telegram:ChannelId` | Target channel for product publishing |
| `Telegram:AdminIds` | Telegram IDs with administrator access |

Never commit `.env`, bot tokens, database passwords, or production settings. See [Security Policy](SECURITY.md).

## Project Status

Current codebase includes domain models, EF Core persistence and initial migration, repositories, Redis FSM, application services, Telegram customer/admin flows, polling/webhook hosting, and Docker assets. Remaining roadmap work centers on testing and hardening, production deployment automation, and the website REST API.

Detailed requirements and roadmap:

- [Business Rules](docs/BUSINESS_RULES.md)
- [Architecture Decisions](docs/ARCHITECTURE.md)
- [Implementation Tasks](docs/TASKS.md)
- [Deployment Guide](docs/DEPLOYMENT.md)
- [Contributing](CONTRIBUTING.md)

## License

Licensed under the [MIT License](LICENSE).

Built by Amir Daneshvar & Claude.
