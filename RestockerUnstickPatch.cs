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
