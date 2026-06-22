# Business Rules

## The Business — What This Is

A **wholesale kitchenware shop** operating entirely through Telegram.
- Target customers: retailers, resellers, bulk buyers — not individual consumers
- Admin sells in bulk; customers always order in quantity
- All transactions are in **Iranian Toman (IRT)**
- The bot is the only storefront — no website yet (comes in Phase 10)

---

## Users & Roles

| Role | Description |
|------|-------------|
| `Customer` | Any Telegram user who starts the bot |
| `Admin` | Defined in `appsettings.json` → `Telegram:AdminIds` array OR `AppUser.Role = Admin` in DB |
| `SuperAdmin` | Reserved — same as Admin for now |

- A user is auto-created in `AppUsers` table on first `/start`
- Banned users (`IsBanned = true`) see a polite rejection message and cannot proceed
- Admin IDs in config override DB role (failsafe: you can always get in)

---

## Products

- Products belong to a `Category` (categories can be nested one level deep: parent → child)
- A product can be **active** or **inactive** (soft delete — never hard delete products with order history)
- Product has a `TelegramFileId` — once an image is sent to Telegram, store this ID to avoid re-uploading
- `Price` on product is the **base unit price** — discounts are calculated separately
- Stock is tracked in `InventoryItem`, not on the product directly (supports multiple warehouses)

---

## Inventory & Warehousing

- One or more `Warehouse` entities
- Each product has one `InventoryItem` per warehouse
- Total available stock = sum of `AvailableQuantity` across all warehouses for that product
- `AvailableQuantity = Quantity - ReservedQuantity`
- Low stock alert threshold is per-`InventoryItem`, default 5 units
- Admin gets notified in bot when any product hits low stock threshold

**Stock lifecycle:**
```
Admin sets stock (e.g. qty = 100)
  → Customer places order for 20
      → reserved += 20 (available = 80)
  → Admin confirms order
      → quantity -= 20, reserved -= 20 (available = 80, total = 80)
  → OR Admin cancels order
      → reserved -= 20 (available = 100 again, total = 100)
```

---

## Quantity Discounts (Wholesale Core Feature)

### Two levels:
1. **Global Tiers** — apply to all products unless overridden
2. **Product Tiers** — specific to one product, completely replaces global for that product

### Tier structure:
```
MinQuantity (int)       — inclusive lower bound
MaxQuantity (int?)      — inclusive upper bound; null = unlimited
DiscountPercent (decimal) — e.g. 5.00 = 5%
IsActive (bool)
DisplayOrder (int)      — for admin UI sorting
```

### Resolution algorithm (in `DiscountService`):
```csharp
1. Load ProductDiscountTiers for this productId where IsActive = true
2. If any exist → use ONLY those (global is ignored entirely)
3. Else → load GlobalDiscountTiers where IsActive = true
4. Find first tier where tier.MinQty <= quantity <= tier.MaxQty (or MaxQty is null)
5. Return tier.DiscountPercent, or 0 if no match
```

### How discount is shown to customer:
- On product detail screen, always show the discount table (even if 0 tiers = "no discount")
- In cart and checkout: show OriginalPrice, DiscountPercent, FinalPrice per item
- Example line:
  ```
  قابلمه استیل × ۲۰ عدد
  قیمت واحد: ۵۰۰٬۰۰۰ تومان
  تخفیف: ۱۰٪ (۵۰٬۰۰۰ تومان)
  جمع: ۹٬۰۰۰٬۰۰۰ تومان
  ```

### Discount in `OrderItem`:
- `OriginalPrice` — snapshot of `Product.Price` at order time
- `DiscountPercent` — snapshot of resolved discount at order time
- `UnitPrice` — `OriginalPrice * (1 - DiscountPercent/100)` — what customer pays
- Never recalculate after order is placed — prices are locked at order time

---

## Orders

### Order placement rules:
1. Check `PaymentSettings.IsShopOpen` → if false, reject with closed message
2. Validate all items have sufficient available stock
3. Create `Order` + `OrderItems` with price/discount snapshots
4. Reserve stock for all items atomically
5. Notify all admins via Telegram

### Payment methods:
- `BankTransfer` — customer transfers money; admin verifies manually
- `Cash` — paid on delivery or in-person pickup
- Admin can enable/disable each independently via `PaymentSettings`
- If both disabled → `IsShopOpen = false` → no orders accepted
- Only enabled methods are shown to customer at checkout

### Delivery types:
- `Shipping` — customer provides address; admin ships
- `InPerson` — customer picks up from physical location

### Order status flow:
```
Pending → Confirmed → Processing → Shipped → Delivered
                 └──────────────────────────► Cancelled (from any stage)
```
- Each status change sends a Telegram notification to the customer
- Admin can attach a note (e.g. tracking number when marking Shipped)

### Customer notifications (Persian):
```
Confirmed:  ✅ سفارش شما تأیید شد.
Processing: 🔄 سفارش شما در حال پردازش است.
Shipped:    🚚 سفارش شما ارسال شد. [note]
Delivered:  ✔️ سفارش شما تحویل داده شد.
Cancelled:  ❌ سفارش شما لغو شد. [note]
```

---

## Payment Settings (Admin Control)

Single row in DB (`PaymentSettings` table — singleton pattern).
Admin can:
- Toggle bank transfer on/off
- Toggle cash on/off
- Set bank name, account number, account holder name
- Set a custom note shown to customer when they choose bank transfer
  (e.g., "لطفاً رسید پرداخت را در واتساپ ارسال کنید")

---

## Channel Publishing

- Admin creates a product in bot → optionally publishes to a Telegram channel
- Channel post contains: product photo + name + description + price + discount table
- Inline buttons on post use **deep links** into the bot:
  - `https://t.me/{BotUsername}?start=product_{productId}` → opens product detail
  - `https://t.me/{BotUsername}?start=buy_{productId}_{qty}` → adds qty to cart directly
- Bot must be an admin of the channel with "Post Messages" permission

---

## Admin Notifications

Admin(s) receive Telegram messages for:
- New order placed (with order summary + customer info)
- Low stock alert (when any product goes below threshold)

All admin IDs in `Telegram:AdminIds` config array receive these notifications.

---

## Pagination

All lists (products, orders, inventory) are paginated.
Default page size: **10 items per page**.
Navigation via inline keyboard buttons: `◄ صفحه قبل` / `صفحه بعد ►`
