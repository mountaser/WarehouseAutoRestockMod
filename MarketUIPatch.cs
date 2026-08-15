using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WarehouseRestockMod
{
    [HarmonyPatch(typeof(MarketShoppingCart), "Start")]
    public static class MarketUIPatch
    {
        public static void Postfix(MarketShoppingCart __instance)
        {
            if (__instance == null) return;

            Transform checkoutBtnTransform = __instance.transform.Find("CheckoutButton");
            if (checkoutBtnTransform == null)
            {
                Button existingBtn = __instance.GetComponentInChildren<Button>();
                if (existingBtn != null)
                {
                    checkoutBtnTransform = existingBtn.transform;
                }
            }

            if (checkoutBtnTransform == null) return;

            GameObject fillBtnObj = GameObject.Instantiate(checkoutBtnTransform.gameObject, checkoutBtnTransform.parent);
            fillBtnObj.name = "FillRackStockButton";

            Text textComp = fillBtnObj.GetComponentInChildren<Text>();
            if (textComp != null)
            {
                textComp.text = "Fill Rack Stock";
            }

            RectTransform rt = fillBtnObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector2 pos = rt.anchoredPosition;
                pos.y -= 45f;
                rt.anchoredPosition = pos;
            }

            Button btn = fillBtnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnFillButtonClick);
            }

            if (Plugin.LogSource != null)
            {
                Plugin.LogSource.LogInfo("Injected 'Fill Rack Stock' button into MarketShoppingCart UI!");
            }
        }

        private static void OnFillButtonClick()
        {
            RestockCalculator.ExecuteRestockOrder();
        }
    }
}
