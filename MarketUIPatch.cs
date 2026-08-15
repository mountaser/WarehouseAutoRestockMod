using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WarehouseRestockMod
{
    [HarmonyPatch(typeof(MarketShoppingCart), "OnEnable")]
    public static class MarketUIPatch
    {
        public static void Postfix(MarketShoppingCart __instance)
        {
            if (__instance == null) return;

            Transform existing = __instance.transform.Find("FillRackStockButton");
            if (existing != null) return;

            GameObject btnObj = new GameObject("FillRackStockButton");
            btnObj.transform.SetParent(__instance.transform, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180f, 45f);
            rt.anchoredPosition = new Vector2(0f, -80f);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.15f, 0.55f, 0.25f, 1f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.15f, 0.55f, 0.25f, 1f);
            cb.highlightedColor = new Color(0.20f, 0.70f, 0.32f, 1f);
            cb.pressedColor = new Color(0.10f, 0.40f, 0.18f, 1f);
            btn.colors = cb;
            btn.onClick.AddListener(OnFillButtonClick);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            Text txt = textObj.AddComponent<Text>();
            txt.text = "FILL RACK STOCK";
            txt.fontSize = 16;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font != null)
            {
                txt.font = font;
            }

            if (Plugin.LogSource != null)
            {
                Plugin.LogSource.LogInfo("Successfully injected standalone 'FILL RACK STOCK' button into MarketShoppingCart!");
            }
        }

        private static void OnFillButtonClick()
        {
            RestockCalculator.ExecuteRestockOrder();
        }
    }
}
