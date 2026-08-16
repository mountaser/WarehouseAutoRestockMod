using System;
using HarmonyLib;

namespace WarehouseRestockMod
{
    // The vanilla save loader (RackManager.LoadRackData -> Rack.Initialize -> RackSlot.Initialize)
    // has no per-rack error handling: if a single saved rack slot throws while spawning its box
    // (e.g. a stale/missing product reference), the whole load loop dies right there and every
    // rack after the failing one is silently never instantiated - even though its save data is fine.
    // These Finalizer patches swallow that exception at the point it happens so the loop can keep
    // going and load the remaining racks, instead of the whole warehouse looking empty.

    [HarmonyPatch(typeof(RackSlot), "Initialize", new Type[] { typeof(Box) })]
    public static class RackSlotInitializeResiliencePatch
    {
        static Exception Finalizer(RackSlot __instance, Box box, Exception __exception)
        {
            if (__exception != null)
            {
                try
                {
                    int productID = (box != null && box.Data != null) ? box.Data.ProductID : -1;
                    Plugin.LogSource?.LogWarning(
                        "Recovered from exception in RackSlot.Initialize (productID=" + productID + "): " + __exception);
                }
                catch { }
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(Rack), "Initialize")]
    public static class RackInitializeResiliencePatch
    {
        static Exception Finalizer(Rack __instance, Exception __exception)
        {
            if (__exception != null)
            {
                try
                {
                    Plugin.LogSource?.LogWarning("Recovered from exception in Rack.Initialize: " + __exception);
                }
                catch { }
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(StorageSectionManager), "Start")]
    public static class StorageSectionManagerStartResiliencePatch
    {
        static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                try
                {
                    Plugin.LogSource?.LogWarning("Recovered from exception in StorageSectionManager.Start: " + __exception);
                }
                catch { }
            }
            return null;
        }
    }
}
