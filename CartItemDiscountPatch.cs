using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace WarehouseRestockMod
{
    [HarmonyPatch(typeof(CartItem), "UpdateUnitPrice")]
    public static class CartItem_UpdateUnitPricePatch
    {
        public static void Postfix(CartItem __instance)
        {
            if (ModConfig.ShowDiscountIndicatorsInUI == null || !ModConfig.ShowDiscountIndicatorsInUI.Value) return;
            if (__instance == null || __instance.m_UnitPriceText == null) return;

            int discountPct = (ModConfig.WholesaleRestockDiscountPercent != null) ? ModConfig.WholesaleRestockDiscountPercent.Value : 0;
            if (discountPct <= 0) return;

            int productID = __instance.ProductID;
            if (productID <= 0) return;

            LocalMarketProductDatabase db = GameObject.FindObjectOfType<LocalMarketProductDatabase>();
            if (db == null) return;

            LocalMarketProductCost costEntry;
            if (db.GetEntryById(productID, out costEntry) && costEntry.Product != null)
            {
                float origPrice = costEntry.Product.BoxPrice;
                float discPrice = origPrice * (1f - (discountPct / 100f));

                __instance.m_UnitPriceText.text = "<s>$" + origPrice.ToString("F2") + "</s> <color=#10B981>$" + discPrice.ToString("F2") + " (-" + discountPct + "% Bulk)</color>";
            }
        }
    }

    [HarmonyPatch(typeof(CartItem), "UpdateTotalPrice")]
    public static class CartItem_UpdateTotalPricePatch
    {
        public static void Postfix(CartItem __instance)
        {
            if (ModConfig.ShowDiscountIndicatorsInUI == null || !ModConfig.ShowDiscountIndicatorsInUI.Value) return;
            if (__instance == null || __instance.m_TotalPriceText == null) return;

            int discountPct = (ModConfig.WholesaleRestockDiscountPercent != null) ? ModConfig.WholesaleRestockDiscountPercent.Value : 0;
            if (discountPct <= 0) return;

            int productID = __instance.ProductID;
            if (productID <= 0) return;

            int count = 1;
            if (__instance.SalesItem != null)
            {
                count = GetCountFromItemQuantity(__instance.SalesItem);
            }

            LocalMarketProductDatabase db = GameObject.FindObjectOfType<LocalMarketProductDatabase>();
            if (db == null) return;

            LocalMarketProductCost costEntry;
            if (db.GetEntryById(productID, out costEntry) && costEntry.Product != null)
            {
                float origTotal = costEntry.Product.BoxPrice * count;
                float discTotal = origTotal * (1f - (discountPct / 100f));

                __instance.m_TotalPriceText.text = "<s>$" + origTotal.ToString("F2") + "</s> <color=#10B981>$" + discTotal.ToString("F2") + " (-" + discountPct + "% Bulk)</color>";
            }
        }

        private static int GetCountFromItemQuantity(ItemQuantity item)
        {
            if (item == null) return 1;
            try
            {
                Type t = item.GetType();
                FieldInfo f = t.GetField("Second", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null) return Convert.ToInt32(f.GetValue(item));
                PropertyInfo p = t.GetProperty("Second", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p != null) return Convert.ToInt32(p.GetValue(item, null));
            }
            catch { }
            return 1;
        }
    }
}
