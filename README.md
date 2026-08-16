# Warehouse Auto-Restock & Direct Delivery Mod (IL2CPP)

A custom **BepInEx 6 (IL2CPP)** plugin for **Supermarket Simulator V1.2.8** that automates warehouse restocking, overrides cart capacity limits, delivers purchased inventory directly onto warehouse rack slots, keeps the warehouse load resilient to bad save data, and adds night ordering plus wholesale/discount tooling to the Market app.

---

## 🌟 Key Features

* 🛒 **1-Click "Fill Rack Stock" Ordering Button:**
  Adds a **"Fill Rack Stock"** button directly to the computer/tablet Market Ordering UI.

* 📦 **Demand & Capacity-Weighted Stock Calculation:**
  Scans all assigned warehouse rack slots (`ProductID > 0`), compares current box count against max capacity, and calculates the exact missing stock needed. Skips unlabeled slots (no physical label placed) since ordering for them wastes cash on stock that can never dock, and deducts loose boxes already sitting on the floor/landing area before calculating what's still needed.

* 💰 **Budget-Capped Cart Population:**
  Clears existing shopping cart items and populates required missing boxes up to your available cash balance (`CapToAvailableCash = true`).

* 🚀 **Cart Capacity Limit Override:**
  Bypasses vanilla shopping cart capacity caps so large restocking orders fit into a single cart (`OverrideMaxCartLimit = true`).

* 🏭 **Direct-to-Warehouse Delivery (Batched):**
  Delivers purchased boxes directly onto assigned warehouse rack slots instead of spawning them outside on the street drop zone (`DirectToWarehouse = true`). Boxes are queued and drained a few at a time per frame (instead of all at once) to avoid overwhelming box placement and to keep large deliveries from causing a lag spike. See **Limitations** below for current placement reliability.

* 🧱 **Warehouse Load Resilience:**
  If a single saved rack slot has stale/bad data, vanilla's save loader normally aborts loading every rack after it. This mod catches that exception per-slot so the rest of the warehouse still loads instead of appearing empty.

* 🌙 **Night Market Ordering:**
  Optionally allows placing Market app orders and receiving instant delivery after 9:00 PM, when the market would vanilla-close for the night (`AllowOrderingAfter9PM`).

* 🏷️ **Wholesale Restock Discount:**
  Applies a configurable discount (0–50%) off market box price when restocking via the Fill Rack Stock button (`WholesaleRestockDiscountPercent`), charged as a real `MoneyManager.MoneyTransition` rather than a display-only price change.

* 💸 **Custom / Overstock Discounts:**
  Apply a configurable custom discount to products, with an option to auto-apply it to overstocked warehouse items (`CustomDiscountPercentage`, `AutoDiscountOverstock`), shown via strikethrough prices and discount badges in the cart and pricing UI (`ShowDiscountIndicatorsInUI`).

* ⌨️ **Optional Hotkeys:**
  Bindable shortcuts for triggering Fill Rack Stock, toggling night ordering, and applying auto-discounts (all default to `None`/off).

---

## 📋 Requirements

* **Game Version:** Supermarket Simulator `V1.2.8 (Build 186)` or newer.
* **Modding Framework:** [BepInEx 6.0.0 (IL2CPP)](https://github.com/BepInEx/BepInEx) for Unity 6000.3.6f1 (.NET 6 CoreCLR).

---

## ⚠️ Known Limitations

* **Direct-to-warehouse delivery only auto-docks a slot's first box, not its second.**
  Rack slots hold up to 2 boxes, but the native call this mod uses to dock a box (`RackSlot.Initialize`) only reliably works going from 0→1 boxes in a slot; a second box into an already-occupied slot silently no-ops instead of erroring. Direct delivery therefore only targets empty slots — any 2nd box per slot is left as a loose floor box for restockers (or the player) to place manually. `RestockCalculator` already deducts loose floor boxes from future orders, so nothing is double-ordered.
* **Restockers can get stuck holding a box when their target rack slot fills up or becomes unavailable mid-task**, with no current way for them to drop it and move on. A fix is planned — see `docs/plans/`.
* **Requires a physical label on the rack slot.** Slots with a `ProductID` assigned but no label placed are skipped by both the stock calculator and direct delivery, since the native slot-initialize call throws on them.
* Compiled against a specific game build (`V1.2.8 Build 186`); native method signatures this mod depends on (`RackSlot.Initialize`, `Restocker.*`, etc.) may change on other game versions.

---

## ⚙️ Configuration (`WarehouseRestock.cfg`)

The mod automatically generates a configuration file in `BepInEx/config/WarehouseRestock.cfg` on first run:

```ini
[General]
## Deliver ordered boxes directly onto assigned warehouse rack slots
DirectToWarehouse = true
## Allow shopping cart capacity to exceed vanilla limit
OverrideMaxCartLimit = true
## Cap restocking orders to available cash balance
CapToAvailableCash = true
## Clear existing cart items before filling missing rack stock
ClearCartBeforeFilling = true

[NightOrdering]
## Allow placing market app orders and instant delivery after 9:00 PM when market closes
AllowOrderingAfter9PM = true

[Wholesale]
## Discount percentage off market box price when restocking via +FILL (0% to 50%)
WholesaleRestockDiscountPercent = 20

[Discounts]
## Custom discount percentage to apply to products (1% to 90%)
CustomDiscountPercentage = 15
## Automatically apply discount to overstocked items in warehouse
AutoDiscountOverstock = false

[UI]
## Display discount badges and strikethrough prices in cart & pricing UI
ShowDiscountIndicatorsInUI = true

[Hotkeys]
## Hotkey to trigger auto-restock calculation (+FILL). Set to None to disable.
RestockHotkey = None
## Hotkey to toggle late-night market ordering after 9:00 PM. Set to None to disable.
NightOrderingToggleHotkey = None
## Hotkey to apply custom discounts on overstocked products. Set to None to disable.
AutoDiscountHotkey = None
```

---

## 🎮 How to Use In-Game

1. Open the **Market** app on your computer or tablet.
2. Click the new **"Fill Rack Stock"** button underneath the Checkout area.
3. The mod will scan all warehouse racks, calculate missing box counts, wipe the cart, and populate it with missing boxes up to your available cash.
4. Click **Checkout**. Boxes deliver directly onto your warehouse rack slots (see Limitations for current placement reliability), and your restockers will pick up on the rest.

---

## 🛠️ Building from Source

To compile `WarehouseRestockMod.dll` manually:

```powershell
$dotnetDir = "path/to/game/dotnet"
$coreDir   = "path/to/game/BepInEx/core"
$interopDir= "path/to/game/BepInEx/interop"
$unityDir  = "path/to/game/BepInEx/unity-libs"

$refs = @(
    "$dotnetDir\System.Private.CoreLib.dll",
    "$dotnetDir\System.Runtime.dll",
    "$dotnetDir\System.Collections.dll",
    "$dotnetDir\netstandard.dll",
    "$dotnetDir\mscorlib.dll",
    "$coreDir\BepInEx.Core.dll",
    "$coreDir\BepInEx.Unity.IL2CPP.dll",
    "$coreDir\Il2CppInterop.Runtime.dll",
    "$coreDir\0Harmony.dll",
    "$interopDir\Il2Cppmscorlib.dll",
    "$interopDir\Assembly-CSharp.dll",
    "$interopDir\PhotonUnityNetworking.dll",
    "$interopDir\UnityEngine.UI.dll",
    "$unityDir\UnityEngine.dll",
    "$unityDir\UnityEngine.CoreModule.dll",
    "$unityDir\UnityEngine.UIModule.dll"
)

$refArgs = $refs | ForEach-Object { "/r:`"$_`"" }
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:library /nostdlib /out:WarehouseRestockMod.dll $refArgs *.cs
```

---

## 📄 License & Credits

* **Developer:** Opencode Agent & deepmind Pair Programming
* Built for **Supermarket Simulator** with **BepInEx 6 IL2CPP**.
