# Warehouse Auto-Restock & Direct Delivery Mod (IL2CPP)

A custom **BepInEx 6 (IL2CPP)** plugin for **Supermarket Simulator V1.2.8** that automates warehouse restocking, overrides cart capacity limits, and delivers purchased inventory directly onto warehouse rack slots while preserving full restocker AI visibility.

---

## 🌟 Key Features

* 🛒 **1-Click "Fill Rack Stock" Ordering Button:**  
  Adds a **"Fill Rack Stock"** button directly to the computer/tablet Market Ordering UI.

* 📦 **Demand & Capacity-Weighted Stock Calculation:**  
  Scans all assigned warehouse rack slots (`ProductID > 0`), compares current box count against max capacity, and calculates the exact missing stock needed.

* 💰 **Budget-Capped Cart Population:**  
  Clears existing shopping cart items and populates required missing boxes up to your available cash balance (`CapToAvailableCash = true`).

* 🚀 **Cart Capacity Limit Override:**  
  Bypasses vanilla shopping cart capacity caps so large restocking orders fit into a single cart (`OverrideMaxCartLimit = true`).

* 🏭 **Direct-to-Warehouse Delivery:**  
  Delivers purchased boxes directly onto assigned warehouse rack slots instead of spawning them outside on the street drop zone (`DirectToWarehouse = true`).

* 🤖 **Restocker AI Target Sync (No Worker Glitches):**  
  Triggers native worker target update events (`r.ResetRestocker()`) upon delivery, guaranteeing restockers immediately recognize and pick up delivered boxes without pathfinding loops.

---

## 📋 Requirements

* **Game Version:** Supermarket Simulator `V1.2.8 (Build 186)` or newer.
* **Modding Framework:** [BepInEx 6.0.0 (IL2CPP)](https://github.com/BepInEx/BepInEx) for Unity 6000.3.6f1 (.NET 6 CoreCLR).

---

## ⚙️ Configuration (`WarehouseRestock.cfg`)

The mod automatically generates a configuration file in `BepInEx/config/WarehouseRestock.cfg` on first run:

```ini
[General]

## Deliver ordered boxes directly onto assigned warehouse rack slots instead of street drop zone
# Setting type: Boolean
# Default value: true
DirectToWarehouse = true

## Allow shopping cart capacity to exceed vanilla limit
# Setting type: Boolean
# Default value: true
OverrideMaxCartLimit = true

## Cap restocking orders to available cash balance
# Setting type: Boolean
# Default value: true
CapToAvailableCash = true

## Clear existing cart items before calculating and filling missing rack stock
# Setting type: Boolean
# Default value: true
ClearCartBeforeFilling = true
```

---

## 🎮 How to Use In-Game

1. Open the **Market** app on your computer or tablet.
2. Click the new **"Fill Rack Stock"** button underneath the Checkout area.
3. The mod will scan all warehouse racks, calculate missing box counts, wipe the cart, and populate it with missing boxes up to your available cash.
4. Click **Checkout**. Boxes deliver directly onto your warehouse rack slots, and your restockers will instantly begin working!

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
