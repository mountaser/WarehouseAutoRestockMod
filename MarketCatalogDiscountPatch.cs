using System;
using HarmonyLib;
using UnityEngine;

namespace WarehouseRestockMod
{
    [HarmonyPatch(typeof(LocalMarketProductPriceDisplay), "UpdateDisplay")]
    public static class LocalMarketProductPriceDisplay_UpdateDisplayPatch
    {
        public static void Postfix(LocalMarketProductPriceDisplay __instance)
        {
            if (ModConfig.ShowDiscountIndicatorsInUI == null || !ModConfig.ShowDiscountIndicatorsInUI.Value) return;
            if (__instance == null || __instance.m_Product == null) return;

            int discountPct = (ModConfig.WholesaleRestockDiscountPercent != null) ? ModConfig.WholesaleRestockDiscountPercent.Value : 0;
            if (discountPct <= 0) return;

            discountPct = Mathf.Clamp(discountPct, 0, 90);

            try
            {
                ProductSO prod = __instance.m_Product.ProductSO;
                if (prod == null) return;

                float origPrice = prod.BoxPrice;
                if (origPrice <= 0f) return;

                float discPrice = origPrice * (1f - (discountPct / 100f));

                if (__instance.m_ActualPrice != null)
                {
                    __instance.m_ActualPrice.text = "<s>$" + origPrice.ToString("F2") + "</s>";
                }

                if (__instance.m_DiscountedPrice != null)
                {
                    __instance.m_DiscountedPrice.text = "<color=#10B981>$" + discPrice.ToString("F2") + " (-" + discountPct + "% Bulk)</color>";
                    __instance.m_DiscountedPrice.gameObject.SetActive(true);
                }
            }
            catch (Exception ex)
            {
                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogWarning("MarketCatalog patch notice: " + ex.Message);
                }
            }
        }
    }
}
