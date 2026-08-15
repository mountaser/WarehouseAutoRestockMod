using System;
using HarmonyLib;
using UnityEngine;
using TMPro;

namespace WarehouseRestockMod
{
    // Shared helpers so every price label fits: auto-size shrink + compact
    // two-line format (original strikethrough on line 1, discounted on line 2).
    public static class DiscountTextFormat
    {
        public static void EnableAutoSize(TMP_Text t)
        {
            if (t == null) return;
            try
            {
                // Cap the max at the label's CURRENT size so auto-size can only
                // SHRINK to fit, never grow the font bigger than the original.
                float cap = t.fontSize;
                if (t.enableAutoSizing && t.fontSizeMax > 0f)
                {
                    cap = t.fontSizeMax; // already configured; keep prior cap
                }
                if (cap <= 0f) cap = 14f;

                t.enableAutoSizing = true;
                t.fontSizeMax = cap;
                t.fontSizeMin = Mathf.Max(4f, cap * 0.4f);
            }
            catch { }
        }

        // Two lines WITH percent (sidebar grand total). Uses bright gold so it
        // stays readable on the sidebar's blue background (green was unreadable).
        // "<s>$ORIG</s>\n<color=#FFD400>$DISC (-PCT%)</color>"
        public static string TwoLine(float orig, float disc, int pct)
        {
            return "<s>$" + orig.ToString("F2") + "</s>\n<color=#FFD400>$" + disc.ToString("F2") + " (-" + pct + "%)</color>";
        }

        // Two lines WITHOUT percent (per-product cart lines, to save space):
        // "<s>$ORIG</s>\n<color=#10B981>$DISC</color>"
        public static string TwoLineNoPct(float orig, float disc)
        {
            return "<s>$" + orig.ToString("F2") + "</s>\n<color=#10B981>$" + disc.ToString("F2") + "</color>";
        }
    }

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

    // Re-applies the bulk badge after the game rewrites m_TotalPriceText
    // (e.g. on quantity +/- changes), so the discount indicator survives.
    [HarmonyPatch(typeof(SalesUIElement), "UpdateTotalPrice")]
    public static class SalesUIElement_UpdateTotalPricePatch
    {
        public static void Postfix(SalesUIElement __instance)
        {
            if (__instance == null) return;
            try
            {
                CartItem cartItem = __instance.TryCast<CartItem>();
                if (cartItem != null)
                {
                    CartItemDiscountUtility.ApplyDiscountFormatting(cartItem);
                }
            }
            catch (Exception ex)
            {
                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogWarning("UpdateTotalPrice patch notice: " + ex.Message);
                }
            }
        }
    }

    // Rewrites the cart GRAND TOTAL text(s) with the discounted total.
    // UpdateTotalPrice (CallerCount 19) is event-driven and never inlined, so
    // this fires only when the cart total recalculates -> zero per-frame cost.
    [HarmonyPatch(typeof(MarketShoppingCart), "UpdateTotalPrice")]
    public static class MarketShoppingCart_UpdateTotalPricePatch
    {
        public static void Postfix(MarketShoppingCart __instance)
        {
            if (__instance == null) return;
            if (ModConfig.ShowDiscountIndicatorsInUI == null || !ModConfig.ShowDiscountIndicatorsInUI.Value) return;

            int discountPct = (ModConfig.WholesaleRestockDiscountPercent != null) ? ModConfig.WholesaleRestockDiscountPercent.Value : 0;
            if (discountPct <= 0) return;
            discountPct = Mathf.Clamp(discountPct, 0, 90);

            try
            {
                float origTotal = 0f;
                var items = __instance.m_CartItems;
                if (items == null) return;

                foreach (CartItem ci in items)
                {
                    if (ci == null) continue;
                    int pid = ci.ProductID;
                    if (pid <= 0 && ci.SalesItem != null) pid = ci.SalesItem.FirstItemID;
                    if (pid <= 0) continue;

                    int cnt = (ci.SalesItem != null) ? ci.SalesItem.FirstItemCount : 1;
                    if (cnt <= 0) cnt = 1;

                    float boxPrice = 0f;
                    if (IDManager.Instance != null)
                    {
                        ProductSO prod = IDManager.Instance.ProductSO(pid);
                        if (prod != null) boxPrice = prod.BoxPrice;
                    }
                    origTotal += boxPrice * cnt;
                }

                if (origTotal <= 0f) return;

                float discTotal = origTotal * (1f - (discountPct / 100f));
                string formatted = DiscountTextFormat.TwoLine(origTotal, discTotal, discountPct);

                var totalTexts = __instance.m_TotalPriceTexts;
                if (totalTexts != null)
                {
                    for (int i = 0; i < totalTexts.Count; i++)
                    {
                        TMP_Text t = totalTexts[i];
                        if (t != null && !t.text.Contains("-" + discountPct + "%"))
                        {
                            DiscountTextFormat.EnableAutoSize(t);
                            t.text = formatted;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource?.LogWarning("Cart total patch notice: " + ex.Message);
            }
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
                productID = item.SalesItem.FirstItemID;
            }
            if (productID <= 0)
            {
                try { productID = item.m_ProductID; } catch { }
            }
            if (productID <= 0) return;

            int count = 1;
            if (item.SalesItem != null)
            {
                count = item.SalesItem.FirstItemCount;
            }
            if (count <= 0)
            {
                try { if (item.m_ProductQuantity != null) count = item.m_ProductQuantity.FirstItemCount; } catch { }
            }
            if (count <= 0) count = 1;

            float unitOrig = 0f;
            try
            {
                if (IDManager.Instance != null)
                {
                    ProductSO prod = IDManager.Instance.ProductSO(productID);
                    if (prod != null) unitOrig = prod.BoxPrice;
                }
            }
            catch (Exception exPrice)
            {
                Plugin.LogSource?.LogWarning("price-lookup error: " + exPrice.Message);
            }

            if (unitOrig <= 0f) return;

            {
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

                // Per-product cart lines omit the (-PCT%) badge to save space;
                // the percent is shown only on the sidebar grand total.
                string unitFormatted = DiscountTextFormat.TwoLineNoPct(unitOrig, unitDisc);
                string totalFormatted = DiscountTextFormat.TwoLineNoPct(totalOrig, totalDisc);

                if (unitText != null && !unitText.text.Contains("#10B981"))
                {
                    DiscountTextFormat.EnableAutoSize(unitText);
                    unitText.text = unitFormatted;
                }

                if (totalText != null && !totalText.text.Contains("#10B981"))
                {
                    DiscountTextFormat.EnableAutoSize(totalText);
                    totalText.text = totalFormatted;
                }
            }
        }

    }
}
