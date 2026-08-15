# Warehouse Auto-Restock & Direct Delivery Mod Implementation Plan

**Goal:** Build, compile, deploy, and verify a C# BepInEx 6 (IL2CPP) plugin for Supermarket Simulator V1.2.8 that adds a 1-click **"Fill Rack Stock"** button to the Market UI, bypasses cart limits, caps orders to available cash, and delivers boxes directly onto warehouse rack slots while preserving full restocker AI visibility.

**Architecture:** A modular C# BepInEx IL2CPP plugin using Harmony patches to hook into `WholeSaleScreen.Start`, `CartManager`, and `DeliverOrder`. Rack scanner iterates `RackSlot` components; cart populator wipes existing cart items and adds needed boxes up to player cash; delivery interceptor uses native `PlaceBoxToRack_Broadcast` calls to attach boxes directly to racks while updating restocker target state.

**Tech Stack:** C# (.NET 6.0 CoreCLR runtime), BepInEx 6 (IL2CPP) v6.0.0-be.755, HarmonyX, Unity 6000.3.6f1 assemblies.

---

### Task Breakdown & Progress
- [x] **Task 1: Plugin Scaffolding & Configuration Manager (`ModConfig.cs` & `Plugin.cs`)**
- [x] **Task 2: Market UI Injection & Cart Limit Bypass (`MarketUIPatch.cs` & `CartLimitPatch.cs`)**
- [x] **Task 3: Rack Scanning & Budget-Capped Cart Populator (`RestockCalculator.cs`)**
- [x] **Task 4: Direct-to-Warehouse Delivery & Restocker Visibility Sync (`DirectDeliveryPatch.cs`)**
- [x] **Task 5: Compilation, Deployment & Verification (`WarehouseRestockMod.dll`)**
