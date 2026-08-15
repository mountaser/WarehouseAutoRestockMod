using System;
using HarmonyLib;
using UnityEngine;

namespace WarehouseRestockMod
{
    // Applies the wholesale discount to the ACTUAL money charged when buying
    // supplies. MoneyManager.MoneyTransition is the real deduction seam
    // (CallerCount 58 -> never inlined), so a Prefix that reduces the amount
    // gated on TransitionType.SUPPLY_COSTS charges the player less for real.
    [HarmonyPatch(typeof(MoneyManager), "MoneyTransition")]
    public static class MoneyTransitionDiscountPatch
    {
        public static void Prefix(ref float amount, MoneyManager.TransitionType type)
        {
            try
            {
                if (type != MoneyManager.TransitionType.SUPPLY_COSTS) return;
                if (ModConfig.WholesaleRestockDiscountPercent == null) return;

                int discountPct = ModConfig.WholesaleRestockDiscountPercent.Value;
                if (discountPct <= 0) return;

                discountPct = Mathf.Clamp(discountPct, 0, 90);

                float orig = amount;
                amount = amount * (1f - (discountPct / 100f));

                Plugin.LogSource?.LogInfo("[Charge] SUPPLY_COSTS orig=$" + orig.ToString("F2") + " new=$" + amount.ToString("F2") + " (-" + discountPct + "% Bulk)");
            }
            catch (Exception ex)
            {
                Plugin.LogSource?.LogWarning("MoneyTransition patch notice: " + ex.Message);
            }
        }
    }
}
