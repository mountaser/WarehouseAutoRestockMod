# Real Wholesale Discount Charge + FPS-Safe Discount UI Implementation Plan

> **For agentic workers:** Steps use checkbox (`- [ ]`) syntax. Implement task-by-task; each task ends with a green build (0 errors) and a deploy. In-game verification is a user checkpoint.

**Goal:** Make the wholesale bulk discount actually deduct less money at checkout (real charge, not just display) and render the discount badge in the cart, cart total, and catalog UI without any FPS drop.

**Architecture:** The previous approach patched `MarketShoppingCart.GetTotalPrice()` (CallerCount=1) and `CartItem.UpdateUnitPrice()` (CallerCount=1) — both single-caller methods that IL2CPP inlines, so their Harmony postfixes never fire. We move the real charge to the verified non-inlined money seam `MoneyManager.MoneyTransition(float amount, TransitionType type, bool)` (CallerCount=58) via a Prefix gated on `TransitionType.SUPPLY_COSTS`, and drive all UI formatting from event-driven, non-inlined anchors (`ReGenerateCartUI` C=4, `MarketShoppingCart.UpdateTotalPrice` C=19, `SalesUIElement.UpdateTotalPrice` C=18, `LocalMarketProductPriceDisplay.UpdateDisplay` C=3). No per-frame polling, so zero FPS impact.

**Tech Stack:** C# / .NET 6.0, BepInEx 6.0.0-be.755 (IL2CPP), HarmonyX, Il2CppInterop, TMPro. Build via `C:\Users\mount\AppData\Local\Temp\opencode\dotnet-sdk\dotnet.exe`.

## Global Constraints

- Game dir: `D:\Supermarket.Simulator.v1.28.186-OFME\Supermarket Simulator - Copy`
- Deploy target: `BepInEx\plugins\WarehouseRestockMod\WarehouseRestockMod.dll`
- Build SDK: `C:\Users\mount\AppData\Local\Temp\opencode\dotnet-sdk\dotnet.exe`
- Discount %: `ModConfig.WholesaleRestockDiscountPercent` (clamp 0..90); UI gate `ModConfig.ShowDiscountIndicatorsInUI`
- Badge format (verbatim): `<s>$ORIG</s> <color=#10B981>$DISC (-PCT% Bulk)</color>`
- Idempotence guard: skip if text already `Contains("Bulk")`
- User decisions (locked): discount applies to the **entire `SUPPLY_COSTS` amount** for **all** Market-app supply purchases (not only +FILL). Real deduction is REQUIRED — display-only is unacceptable.
- Verified IL2CPP members (do NOT change spelling):
  - `MoneyManager.MoneyTransition(float amount, MoneyManager.TransitionType type, bool updateMoneyText = true)` — CallerCount 58
  - `MoneyManager.TransitionType` enum: `NONE, CHECKOUT_INCOME, SUPPLY_COSTS, UPGRADE_COSTS, RENT, BILLS, LOAN_INCOME, LOAN_PAYMENT, STAFF, FURNITURE_SALE, CUSTOMIZATION, FURNITURE_SELL, GAS`
  - `MarketShoppingCart.ReGenerateCartUI()` C=4, `MarketShoppingCart.UpdateTotalPrice()` C=19, `MarketShoppingCart.m_TotalPriceTexts` (Il2CppReferenceArray<TMP_Text>), `MarketShoppingCart.m_CartItems` (List<CartItem>)
  - `SalesUIElement.UpdateTotalPrice()` C=18, `m_UnitPriceText`, `m_TotalPriceText` (TMP_Text), `m_ProductID` (int), `m_ProductQuantity` (ItemQuantity), `m_TotalPrice` (float)
  - `CartItem.ProductID` (int), `CartItem.SalesItem` (ItemQuantity)
  - `ItemQuantity.FirstItemID` (int), `ItemQuantity.FirstItemCount` (int)
  - `LocalMarketProductPriceDisplay.UpdateDisplay()` C=3, fields `m_Product`, `m_ActualPrice`, `m_DiscountedPrice`
  - Pricing: `ProductSO.BoxPrice`; DB lookup `LocalMarketProductDatabase.GetEntryById(int, out LocalMarketProductCost)` → `.Product.BoxPrice`

---

### Task 1: Entry tracing to localize why UI postfixes don't render

**Files:**
- Modify: `CartItemDiscountPatch.cs`
- Modify: `MarketCatalogDiscountPatch.cs`
- Modify: `WholesalePricingPatch.cs`

**Interfaces:**
- Produces: `[TRACE]` log lines proving which postfixes ENTER and which early-return guard trips.

- [ ] **Step 1:** Add `Plugin.LogSource?.LogInfo("[TRACE] <PatchName> ENTER")` as the FIRST statement of every postfix in these files (before any guard).
- [ ] **Step 2:** In `ApplyDiscountFormatting` and catalog `UpdateDisplay`, add a `[TRACE] <name> BAIL:<reason>` line immediately before each `return` guard (`cfg off`, `null instance`, `pct<=0`, `db null`, `GetEntryById false`, `discountedPrice null`).
- [ ] **Step 3:** Build (0 errors), deploy DLL.
- [ ] **Step 4 (USER CHECKPOINT):** User launches game, opens Market catalog, opens cart, adds an item. Then grep `LogOutput.log` for `[TRACE]`.

### Task 2: Real charge discount via MoneyManager.MoneyTransition prefix

**Files:**
- Create: `MoneyTransitionDiscountPatch.cs`
- Modify: `Plugin.cs`

**Interfaces:**
- Consumes: `MoneyManager.MoneyTransition`, `MoneyManager.TransitionType.SUPPLY_COSTS`, `ModConfig.WholesaleRestockDiscountPercent`
- Produces: reduced `amount` on supply purchases (real deduction)

- [ ] **Step 1:** Create `MoneyTransitionDiscountPatch.cs` with a `[HarmonyPatch(typeof(MoneyManager), "MoneyTransition")]` Prefix `Prefix(ref float amount, MoneyManager.TransitionType type)`. When `type == MoneyManager.TransitionType.SUPPLY_COSTS`, discount>0: multiply `amount *= (1f - pct/100f)`. Log `[Charge] SUPPLY_COSTS orig=$X new=$Y (-Z% Bulk)`.
- [ ] **Step 2:** Register `MoneyTransitionDiscountPatch` via `SafePatch` in `Plugin.cs`.
- [ ] **Step 3:** Build (0 errors), deploy DLL.
- [ ] **Step 4 (USER CHECKPOINT):** User buys supplies in Market app; confirm cash drops by the discounted amount and log shows `[Charge]` line.

### Task 3: FPS-safe cart line + grand-total display via event-driven anchors

**Files:**
- Modify: `CartItemDiscountPatch.cs`

**Interfaces:**
- Consumes: `MarketShoppingCart.ReGenerateCartUI`, `MarketShoppingCart.UpdateTotalPrice`, `MarketShoppingCart.m_TotalPriceTexts`, `SalesUIElement.UpdateTotalPrice`
- Produces: badge on each cart line (unit + total) and discounted grand-total text

- [ ] **Step 1:** Keep `MarketShoppingCart_ReGenerateCartUIPatch` and `SalesUIElement_UpdateTotalPricePatch` postfixes as the cart-line drivers; ensure `CartItem_UpdateUnitPricePatch` is no longer relied upon (leave registered but harmless).
- [ ] **Step 2:** Add `MarketShoppingCart_UpdateTotalPricePatch` (Postfix) that recomputes the discounted grand total from `m_CartItems` and rewrites each TMP in `m_TotalPriceTexts` with `<s>$ORIG</s> <color=#10B981>$DISC (-PCT% Bulk)</color>` (guard `Contains("Bulk")`).
- [ ] **Step 3:** Register the new patch in `Plugin.cs`.
- [ ] **Step 4:** Build (0 errors), deploy DLL.
- [ ] **Step 5 (USER CHECKPOINT):** User opens cart, changes quantity; confirm line badges + grand total persist and no FPS drop.

### Task 4: Catalog fix per Task 1 findings

**Files:**
- Modify: `MarketCatalogDiscountPatch.cs`

**Interfaces:**
- Consumes: `LocalMarketProductPriceDisplay.UpdateDisplay`, `m_ActualPrice`, `m_DiscountedPrice`, `ProductSO.BoxPrice`

- [ ] **Step 1:** Apply fix indicated by `[TRACE]` (most likely: if `m_DiscountedPrice` is null, write the full formatted badge into `m_ActualPrice` instead; ensure `SetActive(true)`).
- [ ] **Step 2:** Build (0 errors), deploy DLL.
- [ ] **Step 3 (USER CHECKPOINT):** User opens Market catalog; confirm each card shows strikethrough + green `-PCT% Bulk`.

### Task 5: Clean up tracing, verify, commit + push

**Files:**
- Modify: all patch files (reduce `[TRACE]` to a single config-gated debug line or remove)

- [ ] **Step 1:** Gate/remove `[TRACE]` logs; keep concise `[Charge]` line.
- [ ] **Step 2:** Build (0 errors), deploy DLL.
- [ ] **Step 3 (USER CHECKPOINT):** Final in-game confirm: money charged less, cart + total + catalog badges show, no FPS drop.
- [ ] **Step 4:** `git add` changed files; commit `feat(discount): charge real wholesale discount via MoneyManager.MoneyTransition + FPS-safe event-driven UI badges`; push to `master`.
