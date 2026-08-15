# Design Specification: Warehouse Auto-Restock & Direct Delivery Mod (IL2CPP)

**Game Version:** Supermarket Simulator V1.2.8 (Build 186) - Unity 6000.3.6f1  
**Modding Framework:** BepInEx 6.0.0-be.755 (IL2CPP) - .NET 6.0 CoreCLR  
**Date:** 2026-08-15  

---

## 1. Overview & Objectives
The **Warehouse Auto-Restock & Direct Delivery Mod** is a custom C# plugin built for BepInEx 6 (IL2CPP) on Supermarket Simulator. It streamlines warehouse operations by adding a 1-click **"Fill Rack Stock"** button to the Computer/Tablet Market UI.

### Key Features:
1. **Automated Rack Stock Calculation:** Scans all placed warehouse racks and calculates missing box counts for every assigned product slot (`ProductID > 0`).
2. **Budget-Capped Cart Population:** Clears the existing shopping cart and fills it with required boxes up to the player's available cash balance.
3. **Cart Limit Override:** Bypasses vanilla cart item limits when populating restocking orders.
4. **Direct-to-Warehouse Delivery:** Delivers purchased boxes directly onto assigned warehouse rack slots instead of spawning them outside on the street.
5. **Restocker Sync Fix:** Native placement method calls (`RackSlot.PlaceBoxToRack_Broadcast` / `RackSlot.PlaceBox`) guarantee restockers immediately recognize and handle delivered items.

---

## 2. Architecture & Components

```
+-------------------------------------------------------------------------------+
|                       Market UI (WholeSaleScreen)                             |
|                                                                               |
|   [ Products List ]       [ Cart Area ]      [ Fill Rack Stock Button ]       |
+-------------------------------------------------------------------------------+
                                      | (OnClick)
                                      v
+-------------------------------------------------------------------------------+
|                       Cart Population & Budget Logic                          |
|  1. Clear Cart (CartManager.ClearCart)                                        |
|  2. Calculate missing boxes per RackSlot                                      |
|  3. Bypass cart limit patch (CartMaxed Patch)                                 |
|  4. Add missing boxes while TotalCost <= PlayerCash                           |
+-------------------------------------------------------------------------------+
                                      | (Player Clicks Checkout)
                                      v
+-------------------------------------------------------------------------------+
|                       Direct-to-Rack Delivery Logic                           |
|  1. Intercept Order Delivery (DeliverOrder Patch)                             |
|  2. If DirectToWarehouse == true:                                             |
|     a. For each ordered box, find corresponding assigned RackSlot.           |
|     b. Call RackSlot.PlaceBox() / SpawnBoxInRack() to attach box.             |
|     c. Trigger Rack & RestockerManager update events.                         |
|  3. Fallback: If rack slot fills up mid-delivery, spawn excess on street.     |
+-------------------------------------------------------------------------------+
```

---

## 3. Configuration Options (`WarehouseRestock.cfg`)

```ini
[General]
# Deliver ordered boxes directly onto assigned warehouse rack slots instead of spawning on street
DirectToWarehouse = true

# Allow shopping cart capacity to exceed vanilla limit when populating restocking orders
OverrideMaxCartLimit = true

# Cap restocking orders to available cash balance
CapToAvailableCash = true

# Clear existing cart items before calculating and filling missing rack stock
ClearCartBeforeFilling = true
```
