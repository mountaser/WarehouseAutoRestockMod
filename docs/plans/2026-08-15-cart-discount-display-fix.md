# Cart Discount % Display Fix Implementation Plan

> **For agentic workers:** Steps use checkbox (`- [ ]`) syntax for tracking. Implement task-by-task, verifying each build before moving on.

**Goal:** Make the wholesale bulk discount badge (`<s>$XX.XX</s> <color=#10B981>$YY.YY (-ZZ% Bulk)</color>`) appear on every Market shopping-cart line (unit + total) and make the cart grand total reflect the discount, by fixing broken `ItemQuantity` member access and re-applying the badge after the game rewrites cart text.

**Architecture:** Three Harmony postfix patches on the IL2CPP game types. The current code fails because it reads `ItemQuantity` via managed `System.Reflection` for non-existent members `"First"`/`"Second"`; the real strongly-typed properties are `FirstItemID` and `FirstItemCount`. We replace reflection with direct typed access, add a postfix on `SalesUIElement.UpdateTotalPrice()` so the line-total badge survives quantity changes, and add diagnostic logging to confirm resolution at runtime.

**Tech Stack:** C# / .NET 6.0, BepInEx 6.0.0-be.755 (IL2CPP), HarmonyX, Il2CppInterop, TMPro. Build via temp SDK `C:\Users\mount\AppData\Local\Temp\opencode\dotnet-sdk\dotnet.exe`.

## Global Constraints

- Game dir: `D:\Supermarket.Simulator.v1.28.186-OFME\Supermarket Simulator - Copy`
- Deploy target: `BepInEx\plugins\WarehouseRestockMod\WarehouseRestockMod.dll`
- Build SDK: `C:\Users\mount\AppData\Local\Temp\opencode\dotnet-sdk\dotnet.exe`
- Discount value source: `ModConfig.WholesaleRestockDiscountPercent` (clamp 0..90)
- Badge format (verbatim): `<s>$ORIG</s> <color=#10B981>$DISC (-PCT% Bulk)</color>`
- Guard against double-format with `!text.Contains("Bulk")`
- Verified IL2CPP member names (do NOT change):
  - `CartItem.ProductID` (int prop), `CartItem.SalesItem` (ItemQuantity prop)
  - `ItemQuantity.FirstItemID` (int prop), `ItemQuantity.FirstItemCount` (int prop)
  - `SalesUIElement.m_UnitPriceText`, `m_TotalPriceText` (TMP_Text), `m_ProductID` (int), `m_ProductQuantity` (ItemQuantity)
  - `SalesUIElement.UpdateTotalPrice()`, `CartItem.UpdateUnitPrice()`, `MarketShoppingCart.ReGenerateCartUI()`, `MarketShoppingCart.GetTotalPrice()`
  - Pricing: `LocalMarketProductCost.Product.BoxPrice` via `LocalMarketProductDatabase.GetEntryById`

---

### Task 1: Fix ItemQuantity access + logging in checkout math

**Files:**
- Modify: `WholesalePricingPatch.cs`

**Interfaces:**
- Consumes: `ModConfig.WholesaleRestockDiscountPercent`, `MarketShoppingCart.CartData.ProductInCarts` (IEnumerable<ItemQuantity>), `ItemQuantity.FirstItemID`, `ItemQuantity.FirstItemCount`, `LocalMarketProductDatabase.GetEntryById`
- Produces: corrected `GetTotalPrice` postfix that sets `__result` to discounted total

- [ ] **Step 1:** Remove `GetProductIDFromItemQuantity` / `GetCountFromItemQuantity` reflection helpers; read `itemQty.FirstItemID` and `itemQty.FirstItemCount` directly in the loop.
- [ ] **Step 2:** Add `Plugin.LogSource.LogInfo` reporting original `__result` vs computed discounted total.
- [ ] **Step 3:** Build; expect 0 errors.

### Task 2: Fix ItemQuantity access + logging in cart badge display

**Files:**
- Modify: `CartItemDiscountPatch.cs`

**Interfaces:**
- Consumes: `CartItem.ProductID`, `CartItem.SalesItem.FirstItemCount`, `CartItem.m_ProductID`, `CartItem.m_ProductQuantity.FirstItemCount`, `m_UnitPriceText`, `m_TotalPriceText`
- Produces: `CartItemDiscountUtility.ApplyDiscountFormatting(CartItem)` that formats both texts

- [ ] **Step 1:** Replace reflection helpers with direct typed access: productID from `item.ProductID` (fallback `item.m_ProductID`), count from `item.SalesItem.FirstItemCount` (fallback `item.m_ProductQuantity.FirstItemCount`).
- [ ] **Step 2:** Add `LogInfo` reporting resolved productID, count, unitOrig, unitDisc.
- [ ] **Step 3:** Build; expect 0 errors.

### Task 3: Add UpdateTotalPrice re-apply patch + register + deploy

**Files:**
- Modify: `CartItemDiscountPatch.cs` (new patch class `SalesUIElement_UpdateTotalPricePatch`)
- Modify: `Plugin.cs` (register via `SafePatch`)

**Interfaces:**
- Consumes: `SalesUIElement.UpdateTotalPrice()`, `CartItemDiscountUtility.ApplyDiscountFormatting`
- Produces: postfix that re-applies badge after game rewrites `m_TotalPriceText`

- [ ] **Step 1:** Add `[HarmonyPatch(typeof(SalesUIElement), "UpdateTotalPrice")]` postfix that calls `ApplyDiscountFormatting` on the instance cast to `CartItem` (guard: only when it is a CartItem).
- [ ] **Step 2:** Register `SalesUIElement_UpdateTotalPricePatch` in `Plugin.cs`.
- [ ] **Step 3:** Build (0 errors), deploy DLL to plugins folder.
- [ ] **Step 4:** User verifies in-game + grep log for diagnostics; then commit + push.
