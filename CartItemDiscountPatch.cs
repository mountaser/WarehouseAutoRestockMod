using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace WarehouseRestockMod
{
    [HarmonyPatch(typeof(CartItem), "Setup", new Type[] { typeof(ItemQuantity), typeof(MarketShoppingCart), typeof(SalesType) })]
    public static class CartItem_SetupPatch
    {
        public static void Postfix(CartItem __instance)
        {
            CartItemDiscountUtility.ApplyDiscountFormatting(__instance);
        }
    }

    [HarmonyPatch(typeof(CartItem), "Setup", new Type[] { typeof(ItemQuantity), typeof(SalesType) })]
    public static class CartItem_SetupShortPatch
    {
        public static void Postfix(CartItem __instance)
        {
            CartItemDiscountUtility.ApplyDiscountFormatting(__instance);
        }
    }

    [HarmonyPatch(typeof(CartItem), "UpdateUnitPrice")]
    public static class CartItem_UpdateUnitPricePatch
    {
        public static void Postfix(CartItem __instance)
        {
            CartItemDiscountUtility.ApplyDiscountFormatting(__instance);
        }
    }

    [HarmonyPatch(typeof(CartItem), "UpdateTotalPrice")]
    public static class CartItem_UpdateTotalPricePatch
    {
        public static void Postfix(CartItem __instance)
        {
            CartItemDiscountUtility.ApplyDiscountFormatting(__instance);
        }
    }

    public static class CartItemDiscountUtility
    {
        public static void ApplyDiscountFormatting(CartItem item)
        {
            if (ModConfig.ShowDiscountIndicatorsInUI == null || !ModConfig.ShowDiscountIndicatorsInUI.Value) return;
            if (item == null) return;

            int discountPct = (ModConfig.WholesaleRestockDiscountPercent != null) ? ModConfig.WholesaleRestockDiscountPercent.Value : 0;
            if (discountPct <= 0) return;

            int productID = item.ProductID;
            if (productID <= 0 && item.SalesItem != null)
            {
                productID = GetProductIDFromItemQuantity(item.SalesItem);
            }
            if (productID <= 0) return;

            int count = 1;
            if (item.SalesItem != null)
            {
                count = GetCountFromItemQuantity(item.SalesItem);
            }

            LocalMarketProductDatabase db = GameObject.FindObjectOfType<LocalMarketProductDatabase>();
            if (db == null) return;

            LocalMarketProductCost costEntry;
            if (db.GetEntryById(productID, out costEntry) && costEntry.Product != null)
            {
                float unitOrig = costEntry.Product.BoxPrice;
                float unitDisc = unitOrig * (1f - (discountPct / 100f));

                float totalOrig = unitOrig * count;
                float totalDisc = unitDisc * count;

                if (item.m_UnitPriceText != null)
                {
                    item.m_UnitPriceText.text = "<s>$" + unitOrig.ToString("F2") + "</s> <color=#10B981>$" + unitDisc.ToString("F2") + " (-" + discountPct + "% Bulk)</color>";
                }

                if (item.m_TotalPriceText != null)
                {
                    item.m_TotalPriceText.text = "<s>$" + totalOrig.ToString("F2") + "</s> <color=#10B981>$" + totalDisc.ToString("F2") + " (-" + discountPct + "% Bulk)</color>";
                }
            }
        }

        private static int GetProductIDFromItemQuantity(ItemQuantity item)
        {
            if (item == null) return 0;
            try
            {
                Type t = item.GetType();
                FieldInfo f = t.GetField("First", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null) return Convert.ToInt32(f.GetValue(item));
                PropertyInfo p = t.GetProperty("First", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p != null) return Convert.ToInt32(p.GetValue(item, null));
            }
            catch { }
            return 0;
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
