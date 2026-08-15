using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WarehouseRestockMod
{
    public class RestockUIComponent : MonoBehaviour
    {
        private float lastCheckTime = 0f;

        private void Update()
        {
            // 1. Hotkey F6 trigger anytime in-game
            if (Input.GetKeyDown(KeyCode.F6))
            {
                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogInfo("F6 Hotkey pressed! Executing auto-restock calculation...");
                }
                RestockCalculator.ExecuteRestockOrder();
            }

            // 2. Check UI every 0.2 seconds to minimize performance overhead
            if (Time.time - lastCheckTime > 0.2f)
            {
                lastCheckTime = Time.time;
                CheckAndInjectUIButton();
            }
        }

        private void CheckAndInjectUIButton()
        {
            CartManager cart = CartManager.Instance;
            if (cart == null) return;

            MarketShoppingCart shoppingCart = cart.MarketShoppingCart;
            if (shoppingCart == null)
            {
                shoppingCart = GameObject.FindObjectOfType<MarketShoppingCart>();
            }

            if (shoppingCart == null || !shoppingCart.gameObject.activeInHierarchy) return;

            Transform existing = shoppingCart.transform.Find("FillRackStockButton");
            if (existing != null) return;

            // Construct standalone UI button container
            GameObject btnObj = new GameObject("FillRackStockButton");
            btnObj.transform.SetParent(shoppingCart.transform, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180f, 45f);
            rt.anchoredPosition = new Vector2(0f, -80f);

            // Dark green background image
            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.15f, 0.55f, 0.25f, 1f);

            // Button interactivity & states
            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.15f, 0.55f, 0.25f, 1f);
            cb.highlightedColor = new Color(0.20f, 0.70f, 0.32f, 1f);
            cb.pressedColor = new Color(0.10f, 0.40f, 0.18f, 1f);
            btn.colors = cb;
            btn.onClick.AddListener((UnityAction)OnFillButtonClick);

            // Text GameObject child
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
                Plugin.LogSource.LogInfo("Successfully injected standalone 'FILL RACK STOCK' button via RestockUIComponent!");
            }
        }

        private static void OnFillButtonClick()
        {
            RestockCalculator.ExecuteRestockOrder();
        }
    }
}
