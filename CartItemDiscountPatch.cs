using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using TMPro;

namespace WarehouseRestockMod
{
    [HarmonyPatch(typeof(MarketShoppingCart), "ReGenerateCartUI")]
    public static class MarketShoppingCart_ReGenerateCartUIPatch
    {
        public static void Postfix(MarketShoppingCart __instance)
        {
            CartItemDiscountUtility.FormatAllCartItems(__instance);
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

    public static class CartItemDiscountUtility
    {
        public static void FormatAllCartItems(MarketShoppingCart shoppingCart)
        {
            if (shoppingCart == null) return;
            try
            {
                CartItem[] cartItems = shoppingCart.GetComponentsInChildren<CartItem>(true);
                if (cartItems == null) return;

                foreach (CartItem item in cartItems)
                {
                    ApplyDiscountFormatting(item);
                }
            }
            catch (Exception ex)
            {
                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogWarning("FormatAllCartItems notice: " + ex.Message);
                }
            }
        }

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

                TMP_Text unitText = null;
                TMP_Text totalText = null;

                try { unitText = item.m_UnitPriceText; } catch { }
                try { totalText = item.m_TotalPriceText; } catch { }

                // Fallback to searching child TMP_Text elements on CartItem transform
                TMP_Text[] allTexts = item.GetComponentsInChildren<TMP_Text>(true);
                if (allTexts != null && allTexts.Length > 0)
                {
                    foreach (TMP_Text txt in allTexts)
                    {
                        if (txt == null) continue;
                        string n = txt.name.ToLowerInvariant();
                        if (n.Contains("unit") || n.Contains("cost"))
                        {
                            if (unitText == null) unitText = txt;
                        }
                        else if (n.Contains("total") || n.Contains("price"))
                        {
                            if (totalText == null) totalText = txt;
                        }
                    }

                    if (unitText == null && allTexts.Length >= 2)
                    {
                        unitText = allTexts[allTexts.Length - 2];
                    }
                    if (totalText == null && allTexts.Length >= 1)
                    {
                        totalText = allTexts[allTexts.Length - 1];
                    }
                }

                string unitFormatted = "<s>$" + unitOrig.ToString("F2") + "</s> <color=#10B981>$" + unitDisc.ToString("F2") + " (-" + discountPct + "% Bulk)</color>";
                string totalFormatted = "<s>$" + totalOrig.ToString("F2") + "</s> <color=#10B981>$" + totalDisc.ToString("F2") + " (-" + discountPct + "% Bulk)</color>";

                if (unitText != null && !unitText.text.Contains("Bulk"))
                {
                    unitText.text = unitFormatted;
                }

                if (totalText != null && !totalText.text.Contains("Bulk"))
                {
                    totalText.text = totalFormatted;
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
