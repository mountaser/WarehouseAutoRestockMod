# Market App UI Button Redesign & Raycast Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Redesign `RestockUIComponent` to dock the "FILL RACK STOCK" button neatly in the Market Shopping Cart UI footer with compact `150x36px` dimensions, emerald styling, and raycast click target fixes.

**Architecture:** Update `RestockUIComponent.cs` to locate `MarketShoppingCart`'s button container, append `FillRackStockButton`, configure explicit `Image.raycastTarget` and `Text.raycastTarget = false`, and call `SetAsLastSibling()`.

**Tech Stack:** C# .NET 6.0 CoreCLR, BepInEx 6 IL2CPP, Unity 6 UI (`UnityEngine.UI.Image`, `UnityEngine.UI.Button`, `UnityEngine.UI.Text`).

## Global Constraints
- Target DLL: `BepInEx/plugins/WarehouseRestockMod/WarehouseRestockMod.dll`
- Target SDK: `.NET SDK 6.0.428` (`C:\Users\mount\AppData\Local\Temp\opencode\dotnet-sdk\dotnet.exe`)

---

### Task 1: Refactor `RestockUIComponent.cs` Layout & Raycast Target Binding

**Files:**
- Modify: `Mods/WarehouseRestockMod/RestockUIComponent.cs`

**Steps:**
1. Update `CheckAndInjectUIButton()` in `RestockUIComponent.cs` to search for parent cart footer container (or attach directly to `shoppingCart.transform` with updated anchors `Vector2(0.5f, 0f)` and bottom offset).
2. Set button RectTransform dimensions: `sizeDelta = Vector2(150f, 36f)`, `anchoredPosition = Vector2(0f, 20f)`.
3. Set `Image.raycastTarget = true` on `img`.
4. Set `Text.raycastTarget = false` on `txt` so text mesh doesn't swallow clicks.
5. Invoke `btnObj.transform.SetAsLastSibling()` to ensure button is at top of canvas z-order.

---

### Task 2: Build & Deploy

**Files:**
- Build Target: `Mods/WarehouseRestockMod/WarehouseRestockMod.csproj`
- Output DLL: `BepInEx/plugins/WarehouseRestockMod/WarehouseRestockMod.dll`

**Steps:**
1. Execute `dotnet build` using `C:\Users\mount\AppData\Local\Temp\opencode\dotnet-sdk\dotnet.exe`.
2. Copy compiled `WarehouseRestockMod.dll` to `BepInEx/plugins/WarehouseRestockMod/WarehouseRestockMod.dll`.

---
