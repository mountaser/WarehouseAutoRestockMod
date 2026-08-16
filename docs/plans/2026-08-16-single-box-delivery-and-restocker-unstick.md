# Direct-Delivery Single-Box Fix & Restocker Unstick Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stop direct-to-warehouse delivery from wasting ~94% of its box-placement attempts on already-occupied rack slots, and stop restockers from getting permanently stuck holding a box when their target rack slot is full or otherwise unavailable.

**Architecture:** Both parts are independent, narrow Harmony patches inside the existing `WarehouseRestockMod` BepInEx IL2CPP plugin (`Mods/WarehouseRestockMod/`). Part A tightens an existing condition in `DirectDeliveryPatch.cs`. Part B adds one new patch file (`RestockerUnstickPatch.cs`) that hooks the native `Restocker` class at the exact point it transitions into its "waiting for a rack slot" state, and makes it drop its carried box instead of blocking. Both are gated by `ModConfig` toggles, matching every other behavior in this mod.

**Tech Stack:** C# / .NET 6, BepInEx 6.0.0-be.755 (IL2CPP), HarmonyLib 2.x, Supermarket Simulator V1.2.8 (Build 186), Unity 6000.3.6f1.

## Global Constraints

- Game version: Supermarket Simulator `V1.2.8 (Build 186)`, Unity `6000.3.6f1`, BepInEx `6.0.0-be.755` IL2CPP, .NET 6 CoreCLR — this plan's native method/field names are only valid against this build.
- Every new behavior must be gated by a `ModConfig` `ConfigEntry<bool>`, following the existing pattern in `ModConfig.cs` (`DirectToWarehouse`, `OverrideMaxCartLimit`, etc.) — never hardcode new always-on behavior.
- Every new Harmony patch class must be registered through `Plugin.SafePatch(harmony, typeof(...))` in `Plugin.cs`, never applied directly — `SafePatch` swallows patch-time exceptions so one bad patch can't take down the whole plugin.
- Every call into native/IL2CPP game state must be wrapped in `try/catch` and logged via `Plugin.LogSource` (`LogInfo`/`LogWarning`/`LogError`), matching every existing patch file in this mod.
- **No automated test framework exists in this project** — it is a compiled Unity IL2CPP plugin, not a standalone library. "Testing" in every task below means: build the DLL, deploy it, reproduce the scenario in the running game, and confirm the outcome in `BepInEx/LogOutput.log` (path: `D:\Supermarket.Simulator.v1.28.186-OFME\Supermarket Simulator - Copy\BepInEx\LogOutput.log`). The game must be fully closed before overwriting the deployed DLL (it stays locked while running).
- Before overwriting the deployed DLL at `BepInEx/plugins/WarehouseRestockMod/WarehouseRestockMod.dll`, back up the current one first as `WarehouseRestockMod.dll.bak_pre_<change-name>`, matching the existing backups already in that folder (`.bak_pre_addbox_fix`, `.bak_pre_lag_fix`, etc.) — this preserves rollback points if a change misbehaves in-game.
- Commit after each task, from `Mods/WarehouseRestockMod/` (its own git repo, separate from the game install).

---

## File Structure

- **Modify:** `Mods/WarehouseRestockMod/DirectDeliveryPatch.cs` — `TryDockBoxToRack` currently attempts to dock a box into any slot with `currentBoxes < maxBoxes` (i.e. 0 or 1 boxes already present). The 1-box case always silently fails (confirmed in `BepInEx/LogOutput.log`: every logged `"RackSlot.Initialize() did not actually add the box"` warning has `countAfter == 1`, meaning the slot already held 1 box before the attempt). Restrict targeting to empty slots only.
- **Modify:** `Mods/WarehouseRestockMod/ModConfig.cs` — add one new `ConfigEntry<bool>` for the restocker-unstick feature.
- **Create:** `Mods/WarehouseRestockMod/RestockerUnstickPatch.cs` — new Harmony patch(es) on the native `Restocker` class making a restocker drop its carried box instead of blocking when it can't place it.
- **Modify:** `Mods/WarehouseRestockMod/Plugin.cs` — register the new patch class via `SafePatch`.
- **Modify:** `Mods/WarehouseRestockMod/README.md` — flip the two "planned" limitation notes (added earlier this session) to reflect the shipped fix/feature.

---

## Part A: Fix direct-delivery to stop targeting already-occupied slots

### Task 1: Restrict `TryDockBoxToRack` to empty slots only

**Files:**
- Modify: `Mods/WarehouseRestockMod/DirectDeliveryPatch.cs:146-223` (method `TryDockBoxToRack`)
- Test: manual, via `BepInEx/LogOutput.log`

**Interfaces:**
- Consumes: `Rack`, `RackSlot`, `Box` (native IL2CPP types, already used throughout this file — no new usings needed).
- Produces: no change to `TryDockBoxToRack`'s public signature (`private static bool TryDockBoxToRack(Rack[] racks, Box box, int productID)`); callers (`ProcessQueueBatch`) are unaffected.

- [ ] **Step 1: Record the baseline failure count from the last session's log**

Run (PowerShell, from repo root):

```powershell
Select-String -Path "D:\Supermarket.Simulator.v1.28.186-OFME\Supermarket Simulator - Copy\BepInEx\LogOutput.log" -Pattern "did not actually add the box" | Measure-Object | Select-Object -ExpandProperty Count
```

Expected: a large count (the last recorded session had 202 matching lines across 5 delivery batches, with only 131/2318 boxes ending up successfully docked). Note this number down — it's the "before" baseline you'll compare against after the fix.

- [ ] **Step 2: Read the current condition**

Open `Mods/WarehouseRestockMod/DirectDeliveryPatch.cs` and confirm lines 164-167 currently read:

```csharp
                    int currentBoxes = (slot.Data.RackedBoxDatas != null) ? slot.Data.RackedBoxDatas.Count : 0;
                    int maxBoxes = 2; // Default max box capacity per slot

                    if (currentBoxes < maxBoxes)
```

- [ ] **Step 3: Change the condition to only target empty slots**

Replace that block with:

```csharp
                    int currentBoxes = (slot.Data.RackedBoxDatas != null) ? slot.Data.RackedBoxDatas.Count : 0;

                    // RackSlot.Initialize(box) only reliably works going from 0 -> 1 boxes in a
                    // slot; adding a 2nd box to an already-occupied slot silently no-ops instead
                    // of throwing (confirmed in BepInEx/LogOutput.log: every "did not actually
                    // add the box" warning had countAfter == 1). Only target empty slots here so
                    // direct delivery doesn't burn its per-frame budget retrying a doomed
                    // placement. The 2nd box per slot is left as a loose box for restockers (or
                    // the player) to place manually - RestockCalculator.ExecuteRestockOrder
                    // already deducts loose floor boxes from future orders, so it isn't lost
                    // from accounting, just not auto-docked.
                    if (currentBoxes == 0)
```

- [ ] **Step 4: Update the stale comment above the `try` block**

The comment starting `// RackSlot.AddBox() throws NRE almost every call here, ...` (lines 169-178) still describes the old 2-box-per-slot behavior in its last two sentences. Replace the whole comment block with:

```csharp
                        // RackSlot.AddBox() throws NRE almost every call here, and
                        // BoxGenerator.SpawnBoxInRack(slot.InteractionPosition, ...) never actually
                        // docks anything either (0/936 and 0/567 across two full sessions) - likely
                        // because InteractionPosition/Rotation are the player's stand-point for using
                        // the slot, not a box placement anchor. RackSlot.Initialize(Box) is what
                        // actually works reliably for filling an empty slot: it's the exact call the
                        // vanilla save loader makes for every box. The count-verification below is
                        // kept as a safety net in case Initialize unexpectedly no-ops even on an
                        // empty slot.
```

- [ ] **Step 5: Build**

```powershell
cd "D:\Supermarket.Simulator.v1.28.186-OFME\Supermarket Simulator - Copy\Mods\WarehouseRestockMod"
dotnet build -c Release
```

Expected: `Build succeeded.` with output at `bin\Release\net6.0\WarehouseRestockMod.dll`.

- [ ] **Step 6: Back up the deployed DLL and deploy the new build**

Game must be fully closed first.

```powershell
$plugins = "D:\Supermarket.Simulator.v1.28.186-OFME\Supermarket Simulator - Copy\BepInEx\plugins\WarehouseRestockMod"
Copy-Item "$plugins\WarehouseRestockMod.dll" "$plugins\WarehouseRestockMod.dll.bak_pre_empty_slot_only_fix"
Copy-Item "bin\Release\net6.0\WarehouseRestockMod.dll" "$plugins\WarehouseRestockMod.dll" -Force
```

- [ ] **Step 7: Reproduce and verify in-game**

Launch the game, load the save, open the Market app, click **Fill Rack Stock**, checkout a large order (large enough to include slots that already hold 1 box — any warehouse that's been played on for a while will have these). Let the gradual delivery finish (watch for the `"Direct-to-Warehouse gradual delivery completed!"` log line).

- [ ] **Step 8: Check the result**

Run the same command as Step 1 against the now-updated log, but scoped to only the new session (find the last `"queued"` line's line number first, then search after it):

```powershell
Select-String -Path "D:\Supermarket.Simulator.v1.28.186-OFME\Supermarket Simulator - Copy\BepInEx\LogOutput.log" -Pattern "Direct-to-Warehouse gradual delivery completed" | Select-Object -Last 1
```

Expected: the reported ratio (`Successfully auto-docked X/Y boxes`) is close to `Y` — i.e. most boxes for slots that had at least one truly empty slot available now dock successfully, with `"did not actually add the box"` no longer appearing for this session (any remaining shortfall should only be because every matching slot for that product was already at capacity, which is correct behavior, not a bug).

- [ ] **Step 9: Commit**

```bash
cd "D:/Supermarket.Simulator.v1.28.186-OFME/Supermarket Simulator - Copy/Mods/WarehouseRestockMod"
git add DirectDeliveryPatch.cs
git commit -m "fix(delivery): only auto-dock boxes into empty rack slots

RackSlot.Initialize silently no-ops adding a 2nd box to an already-occupied
slot (confirmed via LogOutput.log: 131/2318 boxes docked last session, every
failure at countAfter==1). Restrict direct delivery to empty slots so it
stops burning its per-frame budget on a placement that can't succeed; the
2nd box per slot is left as a loose floor box, which RestockCalculator
already accounts for in future orders."
```

### Task 2: Update README limitation note for the shipped fix

**Files:**
- Modify: `Mods/WarehouseRestockMod/README.md`

**Interfaces:** none (documentation only).

- [ ] **Step 1: Edit the first bullet under "⚠️ Known Limitations"**

Replace:

```markdown
* **Direct-to-warehouse delivery only reliably fills a slot's first box, not its second.**
  Rack slots hold up to 2 boxes, but the native call this mod uses to dock a box (`RackSlot.Initialize`) only reliably works going from 0→1 boxes in a slot; a second box into an already-occupied slot silently no-ops instead of erroring. In practice this means direct delivery currently docks roughly the first half of a large order and leaves the rest as loose boxes for restockers (or the player) to place manually — confirmed in logs at ~131/2318 boxes (5.6%) successfully auto-docked in one session, almost all failures being "add 2nd box to a slot that already has 1." A fix that scopes direct delivery to empty slots only (trading full slot-packing for a near-100% success rate) is planned — see `docs/plans/`.
```

with:

```markdown
* **Direct-to-warehouse delivery only auto-docks a slot's first box, not its second.**
  Rack slots hold up to 2 boxes, but the native call this mod uses to dock a box (`RackSlot.Initialize`) only reliably works going from 0→1 boxes in a slot; a second box into an already-occupied slot silently no-ops instead of erroring. Direct delivery therefore only targets empty slots — any 2nd box per slot is left as a loose floor box for restockers (or the player) to place manually. `RestockCalculator` already deducts loose floor boxes from future orders, so nothing is double-ordered.
```

- [ ] **Step 2: Commit**

```bash
git add README.md
git commit -m "docs: update README limitation note now that empty-slot-only delivery is shipped"
```

---

## Part B: Make restockers drop a stuck box instead of blocking

### Investigation summary (already done — informs the tasks below)

**OUTCOME:** Part B was NOT implemented as designed below. Both Harmony patches (`GoToWaiting`, `CheckForAvailableRackSlotToPlaceBox`) loaded but never fired at runtime — the `Restocker` class turned out to be a legacy/preview-only type; real workers are driven by `SupermarketSimulator.Clerk.Clerk`. See `RestockerUnstickPoller.cs`/`ClerkUnstickPoller.cs` and commit `b0298a7` for what actually shipped: a position/CarryingBox polling approach.

Inspecting `BepInEx/interop/Assembly-CSharp.dll` metadata (via a reflection-only load, since IL2CPP interop DLLs carry full field/property/method signatures even though bodies are native stubs) found the exact native shape of the restocker AI:

- `RestockerState` is a `System.Enum` with values `IDLE`, `RESTOCKING`, `WAITING_FOR_AVAILABLE_RACK_SLOT`.
- `Restocker : UnityEngine.MonoBehaviour` has, among others:
  - `Boolean CarryingBox { get; set; }` (public property)
  - `Box GetCurrentBox()` (public method)
  - `Void DropBoxToGround()` (public method — no parameters)
  - `Void ResetRestocker()` (public method)
  - `IEnumerator GoToWaiting(RestockerState state)` (private method — the transition into a wait state, including `WAITING_FOR_AVAILABLE_RACK_SLOT`)
  - `Boolean CheckForAvailableRackSlotToPlaceBox()` (private method — polled to decide whether the carried box can be placed)
  - `Int32 RestockerID { get; }` (public property, useful for log messages)

This gives a direct, low-risk patch point: `Restocker.GoToWaiting(RestockerState)` is called right when the game itself decides a restocker should wait because it can't place its box — Harmony can `Prefix` that call and react before the "stuck" coroutine even starts.

### Task 3: Add the config toggle and the drop-box-when-stuck patch

**Files:**
- Modify: `Mods/WarehouseRestockMod/ModConfig.cs`
- Create: `Mods/WarehouseRestockMod/RestockerUnstickPatch.cs`
- Modify: `Mods/WarehouseRestockMod/Plugin.cs`

**Interfaces:**
- Consumes: `Restocker`, `RestockerState` (native IL2CPP types, resolved without a `using` the same way `Rack`/`Box`/`RackSlot` are elsewhere in this mod), `ModConfig.DropBoxWhenRackFull` (produced by this task), `Plugin.LogSource` / `Plugin.SafePatch` (existing).
- Produces: `ModConfig.DropBoxWhenRackFull : ConfigEntry<bool>`; `RestockerDropBoxWhenStuckPatch` (Harmony patch class, registered in `Plugin.cs`).

- [ ] **Step 1: Add the config entry**

In `Mods/WarehouseRestockMod/ModConfig.cs`, add a new field near the other `ConfigEntry<bool>` declarations:

```csharp
        public static ConfigEntry<bool> DropBoxWhenRackFull;
```

and inside `Initialize(ConfigFile config)`, add:

```csharp
            DropBoxWhenRackFull = config.Bind("Restockers", "DropBoxWhenRackFull", true, "When a restocker gets stuck holding a box because its target rack slot is full or otherwise unavailable, make it drop the box and look for a new task instead of blocking");
```

- [ ] **Step 2: Create `RestockerUnstickPatch.cs`**

```csharp
using System;
using HarmonyLib;

namespace WarehouseRestockMod
{
    // When a restocker's target rack slot fills up or becomes unavailable while it's still
    // carrying a box, vanilla routes it into Restocker.GoToWaiting(RestockerState) with
    // state == WAITING_FOR_AVAILABLE_RACK_SLOT, and there's no vanilla path for it to drop
    // that box and pick up other work - it can get stuck standing there holding it. This
    // patch makes it drop the box (Restocker.DropBoxToGround, a public native method) and
    // resets it (Restocker.ResetRestocker, also public) so it immediately looks for a new
    // task. The dropped box lands as a loose Box in the scene, which
    // RestockCalculator.ExecuteRestockOrder already scans and deducts from future restock
    // orders, so it isn't lost from accounting.
    [HarmonyPatch(typeof(Restocker), "GoToWaiting", new Type[] { typeof(RestockerState) })]
    public static class RestockerDropBoxWhenStuckPatch
    {
        static void Prefix(Restocker __instance, RestockerState state)
        {
            if (ModConfig.DropBoxWhenRackFull == null || !ModConfig.DropBoxWhenRackFull.Value) return;
            if (__instance == null) return;
            if (state != RestockerState.WAITING_FOR_AVAILABLE_RACK_SLOT) return;

            try
            {
                if (!__instance.CarryingBox) return;

                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogInfo(
                        "Restocker " + __instance.RestockerID + " is stuck waiting for a rack slot while carrying a box - dropping it instead of blocking.");
                }

                __instance.DropBoxToGround();
                __instance.ResetRestocker();
            }
            catch (Exception ex)
            {
                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogWarning("Error in RestockerDropBoxWhenStuckPatch: " + ex.ToString());
                }
            }
        }
    }

    // Fallback patch point - only wire this up (add the SafePatch line in Plugin.cs) if
    // in-game testing in Task 4 shows RestockerDropBoxWhenStuckPatch's log line never
    // appears when a restocker visibly gets stuck holding a box. CheckForAvailableRackSlotToPlaceBox()
    // is polled repeatedly while the restocker is deciding whether it can place its carried
    // box, so this fires on every poll instead of once on state entry - CarryingBox being
    // false after the first drop keeps it idempotent.
    [HarmonyPatch(typeof(Restocker), "CheckForAvailableRackSlotToPlaceBox")]
    public static class RestockerDropBoxWhenStuckFallbackPatch
    {
        static void Postfix(Restocker __instance, bool __result)
        {
            if (ModConfig.DropBoxWhenRackFull == null || !ModConfig.DropBoxWhenRackFull.Value) return;
            if (__instance == null || __result) return;

            try
            {
                if (!__instance.CarryingBox) return;

                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogInfo(
                        "Restocker " + __instance.RestockerID + " has no available rack slot for its carried box (fallback patch) - dropping it.");
                }

                __instance.DropBoxToGround();
                __instance.ResetRestocker();
            }
            catch (Exception ex)
            {
                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogWarning("Error in RestockerDropBoxWhenStuckFallbackPatch: " + ex.ToString());
                }
            }
        }
    }
}
```

- [ ] **Step 3: Register the primary patch in `Plugin.cs`**

In `Mods/WarehouseRestockMod/Plugin.cs`, inside `Load()`, add after the existing `SafePatch(harmony, typeof(StorageSectionManagerStartResiliencePatch));` line:

```csharp
            SafePatch(harmony, typeof(RestockerDropBoxWhenStuckPatch));
```

Do **not** add a `SafePatch` line for `RestockerDropBoxWhenStuckFallbackPatch` yet — that's the contingency covered in Task 4.

- [ ] **Step 4: Build**

```powershell
cd "D:\Supermarket.Simulator.v1.28.186-OFME\Supermarket Simulator - Copy\Mods\WarehouseRestockMod"
dotnet build -c Release
```

Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add ModConfig.cs RestockerUnstickPatch.cs Plugin.cs
git commit -m "feat(restockers): drop carried box instead of blocking when target rack slot is unavailable

Patches Restocker.GoToWaiting(RestockerState) so that when a restocker is
about to enter WAITING_FOR_AVAILABLE_RACK_SLOT while still carrying a box,
it drops the box (native DropBoxToGround) and resets (ResetRestocker) to
immediately look for a new task instead of standing stuck. Gated by
DropBoxWhenRackFull (default on). Includes a fallback patch on
CheckForAvailableRackSlotToPlaceBox, not yet wired in, in case the primary
hook doesn't fire in practice - see docs/plans/2026-08-16-single-box-delivery-and-restocker-unstick.md Task 4."
```

### Task 4: Verify in-game, apply fallback if needed, update README

**Files:**
- Possibly modify: `Mods/WarehouseRestockMod/Plugin.cs` (only if fallback needed)
- Modify: `Mods/WarehouseRestockMod/README.md`

**Interfaces:** none new.

- [ ] **Step 1: Deploy the build**

Game must be fully closed first.

```powershell
$plugins = "D:\Supermarket.Simulator.v1.28.186-OFME\Supermarket Simulator - Copy\BepInEx\plugins\WarehouseRestockMod"
Copy-Item "$plugins\WarehouseRestockMod.dll" "$plugins\WarehouseRestockMod.dll.bak_pre_restocker_unstick"
Copy-Item "bin\Release\net6.0\WarehouseRestockMod.dll" "$plugins\WarehouseRestockMod.dll" -Force
```

- [ ] **Step 2: Reproduce a stuck restocker**

Launch the game, load the save. Pick one product and fill every rack slot assigned to it to full capacity (2/2 boxes). Then get more of that same product into circulation for a restocker to pick up and try to shelve (e.g. a normal, non-direct delivery, or boxes already sitting loose in the landing area) so an available restocker picks one up and tries to path it to a now-full slot.

- [ ] **Step 3: Check the log for the primary patch**

```powershell
Select-String -Path "D:\Supermarket.Simulator.v1.28.186-OFME\Supermarket Simulator - Copy\BepInEx\LogOutput.log" -Pattern "is stuck waiting for a rack slot while carrying a box" | Select-Object -Last 5
```

Expected: at least one matching line appears once the restocker reaches the full slot, and in-game the restocker visibly drops the box and walks off rather than standing frozen holding it.

- [ ] **Step 4a: If Step 3 found matching log lines** — the primary patch works. Skip to Step 5.

- [ ] **Step 4b: If Step 3 found no matching log lines but the restocker is still visibly stuck** — wire up the fallback:

In `Plugin.cs`, add directly below the `SafePatch(harmony, typeof(RestockerDropBoxWhenStuckPatch));` line:

```csharp
            SafePatch(harmony, typeof(RestockerDropBoxWhenStuckFallbackPatch));
```

Rebuild (Step 4 of Task 3's build command), redeploy (Step 1 above, using `.bak_pre_restocker_unstick_fallback` as the backup suffix instead), and repeat Steps 2-3, this time checking for:

```powershell
Select-String -Path "D:\Supermarket.Simulator.v1.28.186-OFME\Supermarket Simulator - Copy\BepInEx\LogOutput.log" -Pattern "has no available rack slot for its carried box \(fallback patch\)" | Select-Object -Last 5
```

- [ ] **Step 5: Update the README limitation note**

In `Mods/WarehouseRestockMod/README.md`, replace:

```markdown
* **Restockers can get stuck holding a box when their target rack slot fills up or becomes unavailable mid-task**, with no current way for them to drop it and move on. A fix is planned — see `docs/plans/`.
```

with a features bullet instead (move it out of Limitations, since it's now shipped) — add to the **🌟 Key Features** list:

```markdown
* 📤 **Restocker Unstick:**
  If a restocker's target rack slot fills up or becomes unavailable while it's still carrying a box, it drops the box and immediately looks for a new task instead of getting stuck standing there (`DropBoxWhenRackFull`, default on).
```

and remove the now-shipped bullet from **⚠️ Known Limitations**.

- [ ] **Step 6: Commit**

```bash
git add README.md
git commit -m "docs: move restocker-unstick from planned limitation to shipped feature in README"
```

(If Step 4b's fallback was needed, include `Plugin.cs` in this commit too, with message `feat(restockers): wire up CheckForAvailableRackSlotToPlaceBox fallback patch for stuck-restocker unstick` instead, committed separately before the README commit.)

---

## Self-Review Notes

- **Spec coverage:** Task 1 covers the diagnosed direct-delivery capacity bug (0→1 works, 1→2 doesn't). Task 2 and Task 4-Step 5 close the loop on the README limitations added this session. Task 3-4 cover the requested "restocker drops the box instead of getting stuck" feature, including a concrete (not hand-waved) fallback if the primary Harmony hook doesn't fire as expected against the real game build.
- **Placeholder scan:** No TBD/TODO markers; every code step has full, copy-pasteable C# or PowerShell; the fallback branch has real code, not a "figure it out later" note.
- **Type consistency:** `TryDockBoxToRack(Rack[] racks, Box box, int productID)` signature unchanged in Task 1. `ModConfig.DropBoxWhenRackFull` name matches between Task 3 Step 1 (declaration) and Task 3 Step 2 (usage in both patch classes). `Restocker`, `RestockerState`, `CarryingBox`, `DropBoxToGround`, `ResetRestocker`, `RestockerID`, `CheckForAvailableRackSlotToPlaceBox` all match the exact names found in the `Assembly-CSharp.dll` metadata dump.
