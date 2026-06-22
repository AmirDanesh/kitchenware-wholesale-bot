# Contributing to KitchenwareBot

Thank you for your interest in contributing! This document outlines how to help make KitchenwareBot better.

## 🎯 Code of Conduct

By participating, you agree to uphold our [Code of Conduct](./CODE_OF_CONDUCT.md). Be respectful, inclusive, and constructive.

---

## 🐛 Reporting Bugs

### Before Submitting
- Check [existing issues](../../issues) for duplicates
- Verify the bug isn't environment-specific (local setup issue)
- Test on latest `main` branch

### Issue Template
```markdown
**Describe the bug:**
A clear description of what the bug is.

**Steps to reproduce:**
1. Step 1
2. Step 2
3. ...

**Expected behavior:**
What should happen?

**Actual behavior:**
What actually happens?

**Environment:**
- OS: Windows 11 / macOS / Linux
- .NET version: 8.0
- Telegram Bot API: v21.x

**Screenshots/logs:**
If applicable, attach error messages or console output.
```

---

## 💡 Suggesting Features

### Before Submitting
- Check [discussions](../../discussions) and [issues](../../issues)
- Consider if the feature fits the project scope (B2B wholesale)
- Align with existing architecture (clean architecture, service layer)

### Feature Request Template
```markdown
**Feature:**
Clear, concise name.

**Problem it solves:**
What pain point does this address?

**Proposed solution:**
How would you implement this?

**Alternatives considered:**
Any other approaches?

**Impact:**
- Customer-facing? Admin-only? Backend?
- Breaking changes? Performance impact?
```

---

## 🔧 Development Setup

### 1. Fork & Clone
```bash
git clone https://github.com/YOUR-USERNAME/kitchenware-wholesale-bot.git
cd kitchenware-wholesale-bot
git remote add upstream https://github.com/ORIGINAL-OWNER/kitchenware-wholesale-bot.git
```

### 2. Create a Branch
```bash
git checkout -b feature/your-feature-name
# or
git checkout -b fix/issue-number-brief-description
```

**Branch naming:**
- `feature/` — new functionality
- `fix/` — bug fixes
- `docs/` — documentation
- `refactor/` — code cleanup (no behavior change)
- `test/` — tests only

### 3. Set Up Local Environment
```bash
# Start dependencies
docker run -d -p 6379:6379 redis:alpine

# Configure secrets (Development only)
# Edit src/Bot/appsettings.Development.json with your BotToken & AdminIds

# Initialize database
dotnet ef database update \
  --project src/KitchenwareBot.Infrastructure \
  --startup-project src/KitchenwareBot.Bot

# Run the bot
dotnet run --project src/KitchenwareBot.Bot
```

---

## ✍️ Coding Standards

### C# Style Guide
- **Naming:** PascalCase for classes/methods, camelCase for variables
- **Interfaces:** `I` prefix (e.g., `IProductService`)
- **Async methods:** `Async` suffix (e.g., `GetProductAsync`)
- **Constants:** `PascalCase` or `UPPER_SNAKE_CASE`

```csharp
// ✅ Good
public interface IProductService
{
    Task<Product> GetProductAsync(Guid id);
}

// ❌ Bad
public interface productService
{
    Task<Product> GetProduct(Guid id);
}
```

### Architecture Rules (Non-Negotiable)
1. **Domain layer** — Zero dependencies. Entities only.
2. **Application** — Business logic. Imports Domain only.
3. **Infrastructure** — EF Core, Redis, repositories. Imports Application + Domain.
4. **Bot** — Telegram handlers. Imports Application + Infrastructure.

### Persian Text
- All user-facing text must be in Persian (Farsi)
- Store strings in `BotMessages.cs` constants
- Use `PriceFormatter.FormatToman(decimal)` for prices

```csharp
// ✅ Good
public const string Welcome = "سلام {0}! 👋 به فروشگاه خوش آمدید.";

// ❌ Bad
message = "Hello " + userName + "! Welcome!";
```

### No MediatR
- Use plain **Service Classes** injected via DI
- This keeps code simple, testable, and license-free
- Example: `IProductService` → `ProductService`

---

## 📝 Commit Guidelines

### Message Format
```
[type] Brief description under 50 chars

Optional detailed explanation. Wrap at 72 characters.
Explain WHY, not WHAT (code shows the what).

Closes #123
```

**Types:**
- `feat:` — new feature
- `fix:` — bug fix
- `docs:` — documentation
- `refactor:` — code cleanup
- `test:` — tests only
- `chore:` — dependencies, tooling

### Examples
```
feat: add quantity discount tier management for admins

Implement UI for global and per-product discount tiers.
Allow admins to create, edit, and delete tiers.
Discount resolution follows: product tiers override global.

Closes #42
```

```
fix: resolve low-stock notification race condition

Previously, concurrent orders could trigger duplicate alerts.
Now using atomic inventory operations with Redis lock.
```

---

## 🧪 Testing

### Before Submitting a PR
```bash
# Build solution
dotnet build

# Run unit tests (when added)
dotnet test

# Code analysis (if configured)
dotnet analyzer
```

### Manual Testing Checklist
- [ ] Feature works in polling mode (dev)
- [ ] Feature works in webhook mode (if applicable)
- [ ] Persists correctly to SQL Server
- [ ] Handles edge cases (empty stock, no discounts, etc.)
- [ ] Persian text displays correctly
- [ ] Prices format correctly with `FormatToman()`
- [ ] Admin notifications send properly

---

## 📤 Submitting a Pull Request

### Before Pushing
```bash
# Fetch latest upstream changes
git fetch upstream
git rebase upstream/main

# Build & test locally
dotnet build
dotnet test
```

### PR Description Template
```markdown
## Summary
Brief description of what this PR does.

## Changes
- Feature A
- Feature B
- Fixed bug C

## Related Issues
Closes #123, relates to #456

## Testing
- [x] Tested locally in polling mode
- [x] Verified Persian text displays correctly
- [x] Confirmed discount logic with quantity X

## Screenshots
If UI changes, attach before/after screenshots.

## Checklist
- [x] Code follows style guide
- [x] No secrets in code/commits
- [x] Proper error handling
- [x] Tests pass
```

### Review Process
1. **Automated checks** — build, linting
2. **Code review** — 1–2 maintainers review
3. **Feedback** — address requests or discuss
4. **Approval & merge** — maintainers merge when approved

---

## 🚀 Getting Help

- 📖 **Docs:** See [docs/](../docs/) folder
- 💬 **Questions?** Open a [Discussion](../../discussions)
- 🐛 **Bug?** File an [Issue](../../issues)
- 📧 **Email:** maintainer@example.com

---

## 📚 Implementation Phases

New contributors should review [TASKS.md](../docs/TASKS.md) to understand:
- Overall project roadmap (10 phases)
- Which phase is currently active
- What tasks are available

Pick a task that interests you, or reach out for guidance!

---

## 🎁 License

By contributing, you agree that your contributions are licensed under the project's MIT License.

---

**Happy coding! 🎉**
