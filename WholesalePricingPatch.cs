using System;
using HarmonyLib;
using UnityEngine;

namespace WarehouseRestockMod
{
    [HarmonyPatch(typeof(MarketShoppingCart), "GetTotalPrice")]
    public static class MarketShoppingCart_GetTotalPricePatch
    {
        public static void Postfix(MarketShoppingCart __instance, ref float __result)
        {
            if (__instance == null) return;
            if (ModConfig.WholesaleRestockDiscountPercent == null) return;

            int discountPct = ModConfig.WholesaleRestockDiscountPercent.Value;
            if (discountPct <= 0) return;

            discountPct = Mathf.Clamp(discountPct, 0, 90);

            try
            {
                CartData cartData = __instance.CartData;
                if (cartData == null || cartData.ProductInCarts == null || cartData.ProductInCarts.Count == 0) return;

                LocalMarketProductDatabase db = GameObject.FindObjectOfType<LocalMarketProductDatabase>();
                if (db == null) return;

                float discountedTotal = 0f;

                foreach (ItemQuantity itemQty in cartData.ProductInCarts)
                {
                    if (itemQty == null) continue;
                    int pId = itemQty.FirstItemID;
                    int count = itemQty.FirstItemCount;

                    if (pId <= 0 || count <= 0) continue;

                    float pricePerBox = 10f;
                    LocalMarketProductCost costEntry;
                    if (db.GetEntryById(pId, out costEntry) && costEntry.Product != null)
                    {
                        pricePerBox = costEntry.Product.BoxPrice;
                    }

                    float discBoxPrice = pricePerBox * (1f - (discountPct / 100f));
                    discountedTotal += discBoxPrice * count;
                }

                __result = discountedTotal;
            }
            catch (Exception ex)
            {
                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogWarning("GetTotalPrice patch notice: " + ex.Message);
                }
            }
        }
    }
}
