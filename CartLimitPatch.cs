using System;
using HarmonyLib;

namespace WarehouseRestockMod
{
    [HarmonyPatch(typeof(CartManager), "CartMaxed", new Type[] { typeof(bool) })]
    public static class CartLimitPatch_CartMaxed
    {
        public static bool Prefix(ref bool __result)
        {
            if (ModConfig.OverrideMaxCartLimit != null && ModConfig.OverrideMaxCartLimit.Value)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CartManager), "CartMaxedPassive", new Type[] { typeof(bool) })]
    public static class CartLimitPatch_CartMaxedPassive
    {
        public static bool Prefix(ref bool __result)
        {
            if (ModConfig.OverrideMaxCartLimit != null && ModConfig.OverrideMaxCartLimit.Value)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
