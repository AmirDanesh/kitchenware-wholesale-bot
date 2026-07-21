# Repository Guidelines

## Project Structure & Module Organization

Run repository-level commands from the solution root (`../..` from this directory). `KitchenwareBot.sln` groups five .NET 8 projects under `src/`:

- `Domain` contains entities, enums, exceptions, and repository contracts. Keep it independent of other projects.
- `Application` contains DTOs, validators, service contracts/implementations, formatting, and session abstractions.
- `Infrastructure` implements EF Core SQL Server persistence, migrations, repositories, and Redis state.
- `Bot` hosts the Telegram bot, with handlers, routing, keyboards, and notifications.
- `API` is the ASP.NET Core HTTP entry point; local request samples live in `KitchenwareBot.API.http`.

There is no dedicated asset or test tree. Never commit generated `bin/`, `obj/`, or `.vs/` content.

## Build, Test, and Development Commands

- `dotnet restore KitchenwareBot.sln` restores all project dependencies.
- `dotnet build KitchenwareBot.sln -c Release` compiles the full solution with release settings.
- `dotnet run --project src/API/KitchenwareBot.API.csproj` starts the API; Development profiles expose Swagger at `http://localhost:5112/swagger`.
- `dotnet run --project src/Bot/KitchenwareBot.Bot.csproj` starts the Telegram host and requires configured SQL Server, Redis, and Telegram credentials.
- `dotnet format KitchenwareBot.sln --verify-no-changes` checks SDK-default C# formatting.

## Coding Style & Naming Conventions

Use four-space indentation, braces on new lines, file-scoped namespaces, nullable-aware code, and implicit usings. Follow existing C# naming: `PascalCase` for types and public members, `IService` for interfaces, `_camelCase` for private fields, and `camelCase` for locals and parameters. Suffix data-transfer records with `Dto` and asynchronous methods with `Async`; pass cancellation tokens as `ct` where practical. No repository-specific analyzer is configured, so keep warnings at zero and run the formatter before review.

## Testing Guidelines

No automated test project, framework, coverage gate, or naming rule is currently configured. Add tests under `tests/KitchenwareBot.<Layer>.Tests`, name classes `<Subject>Tests` and cases `Method_Scenario_ExpectedResult`, add projects to the solution, then run `dotnet test KitchenwareBot.sln`.

## Commit & Pull Request Guidelines

Git history and PR templates are absent from this checkout. Use concise imperative subjects, preferably Conventional Commit prefixes such as `feat:`, `fix:`, or `refactor:`. PRs should explain scope and behavior, link relevant issues, list verification commands/results, and call out migrations or configuration changes. Include screenshots only for visible UI or Swagger changes.

## Security & Configuration

Copy `.env.example` for configuration guidance, but keep `.env`, local appsettings files, tokens, connection strings, and JWT secrets uncommitted. Prefer environment variables or .NET user secrets for development credentials.
