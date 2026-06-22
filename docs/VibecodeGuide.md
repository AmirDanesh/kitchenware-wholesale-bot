# Working with Claude Code — Vibecoding Guide
> How to implement KitchenwareBot effectively with AI assistance

---

## Claude Code vs Claude CoWork — Which to Use

| | Claude Code | Claude CoWork |
|--|-------------|---------------|
| **What it is** | CLI tool in your terminal | Desktop GUI app |
| **Best for** | Writing code, running commands, creating files, migrations, tests | File organization, automation for non-coders |
| **Reads CLAUDE.md?** | ✅ Yes — automatically on every session | ❌ No |
| **Runs dotnet commands?** | ✅ Yes | Limited |
| **For this project** | ✅ **Use this** | Not the right tool for coding |

**Verdict:** For implementing KitchenwareBot, use **Claude Code exclusively**.
CoWork is designed for non-developers managing files and tasks — not for C# development.

---

## How Claude Code Works

```
You open terminal in project root
         │
         ▼
claude (command)
         │
         ▼
Claude Code reads CLAUDE.md automatically  ← this is its "brain" for your project
         │
         ▼
You give it a task ("implement T-17")
         │
         ▼
Claude Code reads relevant files, writes code, runs commands
         │
         ▼
Session ends → next session re-reads CLAUDE.md from scratch
```

Every session starts fresh — CLAUDE.md is what carries knowledge across sessions.
This is why the file structure you created matters so much.

---

## Setup — One Time

```bash
# 1. Install Claude Code (requires Node.js 18+)
npm install -g @anthropic-ai/claude-code

# 2. Navigate to your project root
cd C:\Projects\KitchenwareBot

# 3. Place these files in the project root:
#    CLAUDE.md          ← already created
#    docs/BUSINESS_RULES.md
#    docs/ARCHITECTURE.md
#    docs/TASKS.md      ← copy of your TaskPlan.md

# 4. Start Claude Code
claude
```

---

## How to Prompt Claude Code Per Task

The TaskPlan has 70 tasks with IDs like T-01, T-17, T-47.
Here is the exact prompt pattern that gets the best results:

### Pattern A — Single task
```
Implement T-17 from docs/TASKS.md.
Read CLAUDE.md and docs/ARCHITECTURE.md first before writing any code.
```

### Pattern B — Task with context
```
Implement T-22 (DiscountRepository) from docs/TASKS.md.
The resolution logic is in docs/BUSINESS_RULES.md under "Resolution algorithm".
Put it in src/KitchenwareBot.Infrastructure/Persistence/Repositories/DiscountRepository.cs
```

### Pattern C — Ask before doing
```
Read T-35 and T-36 from docs/TASKS.md.
Before writing any code, explain to me what you're going to create.
I'll approve, then you implement.
```

### Pattern D — Fix or continue
```
The InventoryService.ReserveAsync() throws a NullReferenceException when
the product has no warehouse. Fix it. The code is in
src/KitchenwareBot.Application/Services/InventoryService.cs
```

### What NOT to say
```
❌ "Build the whole project"          → too vague, Claude will make wrong decisions
❌ "Write all the handlers"           → too broad, result will be inconsistent
❌ "You know what to do"              → it doesn't, without reference to TASKS.md
❌ Huge prompts with everything       → dilutes focus, worse output quality
```

---

## Session Workflow — Every Time You Work

```
1. Open terminal in project root
2. Run: claude
3. Claude reads CLAUDE.md automatically ← confirm with /memory command
4. Say: "Continue from T-[X]. Read CLAUDE.md first."
5. Work through 1-3 tasks per session
6. After each task: BUILD and TEST before moving on
   dotnet build src/KitchenwareBot.Bot
7. Mark task done in docs/TASKS.md (change [ ] to [x])
8. If Claude made any architectural decision → update CLAUDE.md
```

---

## The CLAUDE.md File System (Know This)

```
Project root/
├── CLAUDE.md                  ← loaded EVERY session automatically
├── docs/
│   ├── BUSINESS_RULES.md      ← loaded via @import in CLAUDE.md
│   ├── ARCHITECTURE.md        ← loaded via @import in CLAUDE.md
│   └── TASKS.md               ← loaded via @import in CLAUDE.md
└── src/
    ├── Domain/
    │   └── CLAUDE.md          ← loaded when Claude touches any file here
    ├── Application/
    │   └── CLAUDE.md          ← loaded when Claude touches any file here
    ├── Infrastructure/
    │   └── CLAUDE.md          ← loaded when Claude touches any file here
    └── Bot/
        └── CLAUDE.md          ← loaded when Claude touches any file here
```

### What to put in subdirectory CLAUDE.md files

Create these later, as you work in each layer. Examples:

**`src/Domain/CLAUDE.md`:**
```markdown
# Domain Layer Rules
- Entities are in KitchenwareBot.Domain.Entities namespace
- No external dependencies
- Use private setters + factory methods (Product.Create(), Order.Create())
- All business methods on entities return void, throw exceptions on invalid state
```

**`src/Bot/CLAUDE.md`:**
```markdown
# Bot Layer Rules
- Handlers are in KitchenwareBot.Bot.Handlers namespace
- Customer handlers: src/Bot/Handlers/Customer/
- Admin handlers: src/Bot/Handlers/Admin/
- NEVER call services directly from inline button callbacks without state validation
- Always save UserSession BEFORE calling bot.SendMessageAsync
- All Telegram API calls must be inside try/catch
```

---

## Keeping CLAUDE.md Accurate — The Key Habit

CLAUDE.md is only useful if it reflects reality.
Update it whenever:

| Event | What to update |
|-------|---------------|
| You add a new service class | Add it to "Service Layer" list in CLAUDE.md |
| You change a business rule | Update docs/BUSINESS_RULES.md |
| You make an architecture decision | Add to docs/ARCHITECTURE.md with reason |
| You finish a phase | Update "Current Status" in CLAUDE.md |
| You add a new Redis key | Add to "Redis Key Convention" in ARCHITECTURE.md |
| A task is done | Mark `[x]` in docs/TASKS.md |

Tell Claude Code to update CLAUDE.md too:
```
Update docs/TASKS.md to mark T-17 as complete [x].
Also add DiscountRepository to the service layer list in CLAUDE.md.
```

---

## Vibecoding Principles — For Long-Term Expandability

### 1. Interfaces are Claude's map
Every service class has an interface (`IProductService`).
When Claude opens any handler file and sees `IProductService`, it knows exactly
what methods exist without reading the implementation. Always define interfaces first.

### 2. One task = one session focus
Claude Code is best when it does one concrete thing per session.
"Implement the CheckoutHandler" > "Implement all customer handlers"

### 3. Write the "why" in comments for business logic
```csharp
// Discount is resolved at order time and stored as a snapshot.
// Never recalculate after placement — prices are locked.
// See docs/BUSINESS_RULES.md "Discount in OrderItem"
var discountPercent = await _discountService.ResolveDiscountAsync(item.ProductId, item.Qty);
```
When Claude reads this code later, it understands the intent and won't "fix" it.

### 4. Consistent naming teaches Claude your patterns
If your first 5 handlers follow the same structure, Claude will follow that structure
for the next 20 without being told. Consistency compounds.

### 5. Small files over large files
A 600-line handler file is hard for Claude to reason about.
Split: `CheckoutHandler.cs` + `CheckoutKeyboards.cs` + `CheckoutMessages.cs`
Claude can load and reason about each part independently.

### 6. Always build after each task
```bash
dotnet build
```
Never move to the next task with a broken build. Errors compound fast.

### 7. The /memory command is your debugger
When Claude Code seems confused about the project:
```
/memory
```
This shows exactly what files are loaded in its context. If CLAUDE.md isn't listed,
it hasn't been loaded — restart the session from the project root.

### 8. Phase completions are checkpoints
After finishing each phase (Phase 1 scaffold, Phase 2 domain, etc.):
- Run `dotnet build` — zero errors
- Commit to git: `git commit -m "Phase 2 complete: Domain entities"`
- Update "Current Status" in CLAUDE.md
- Start next phase fresh

---

## When Something Goes Wrong

### Claude writes code that doesn't match your architecture
```
Stop. Read CLAUDE.md and docs/ARCHITECTURE.md.
The code you wrote violates rule: [specific rule].
Rewrite [specific file] following the architecture decisions.
```

### Claude forgot what you discussed earlier in the session
```
/compact
```
This compresses the conversation. Claude re-reads CLAUDE.md and continues.

### Claude is inventing things not in the task plan
```
Only implement what is described in T-[X].
Do not add anything beyond what is specified.
If something is unclear, ask me before coding.
```

### The session context is too full
End the session. Commit what you have. Start fresh. Claude re-reads CLAUDE.md.

---

## Quick Reference Card

```
Start session:      claude  (from project root)
Check memory:       /memory
Compress context:   /compact
Reference a task:   "Implement T-17 from docs/TASKS.md"
Build check:        dotnet build src/KitchenwareBot.Bot
Run migrations:     dotnet ef database update --project src/KitchenwareBot.Infrastructure --startup-project src/KitchenwareBot.Bot
Mark task done:     Edit docs/TASKS.md, change [ ] to [x]
```
