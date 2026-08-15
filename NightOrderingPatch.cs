using System;
using HarmonyLib;
using UnityEngine;

namespace WarehouseRestockMod
{
    [HarmonyPatch(typeof(MarketShoppingCart), "get_TooLateToOrderGoods")]
    public static class MarketShoppingCart_TooLateToOrderGoodsPatch
    {
        public static bool Prefix(ref bool __result)
        {
            if (ModConfig.AllowOrderingAfter9PM != null && ModConfig.AllowOrderingAfter9PM.Value)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(MarketShoppingCart), "get_CloseMarket")]
    public static class MarketShoppingCart_CloseMarketPatch
    {
        public static bool Prefix(ref bool __result)
        {
            if (ModConfig.AllowOrderingAfter9PM != null && ModConfig.AllowOrderingAfter9PM.Value)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(MarketShoppingCart), "TimeCheck")]
    public static class MarketShoppingCart_TimeCheckPatch
    {
        public static void Postfix(MarketShoppingCart __instance)
        {
            if (ModConfig.AllowOrderingAfter9PM != null && ModConfig.AllowOrderingAfter9PM.Value && __instance != null)
            {
                try
                {
                    __instance.m_MarketClosed = false;
                    __instance.m_canPurchase = true;

                    if (__instance.m_ClosedMarketText != null && __instance.m_ClosedMarketText.activeSelf)
                    {
                        __instance.m_ClosedMarketText.SetActive(false);
                    }

                    if (__instance.m_PurchaseButton != null)
                    {
                        __instance.m_PurchaseButton.interactable = true;
                    }
                }
                catch (Exception ex)
                {
                    if (Plugin.LogSource != null)
                    {
                        Plugin.LogSource.LogWarning("NightOrdering TimeCheck patch notice: " + ex.Message);
                    }
                }
            }
        }
    }
}
