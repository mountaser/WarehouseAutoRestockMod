using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Il2CppInterop.Runtime;

namespace WarehouseRestockMod
{
    public class RestockUIComponent : MonoBehaviour
    {
        private float lastCheckTime = 0f;
        private bool hasLoggedInitialScan = false;

        private void Update()
        {
            try
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
            catch (Exception ex)
            {
                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogError("[RestockUIComponent] Error in Update loop: " + ex);
                }
            }
        }

        private void CheckAndInjectUIButton()
        {
            try
            {
                CartManager cart = CartManager.Instance;
                if (cart == null) return;

                MarketShoppingCart shoppingCart = cart.MarketShoppingCart;
                if (shoppingCart == null)
                {
                    shoppingCart = GameObject.FindObjectOfType<MarketShoppingCart>();
                }

                if (shoppingCart == null || !shoppingCart.gameObject.activeInHierarchy) return;

                if (!hasLoggedInitialScan && Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogInfo("[RestockUIComponent] Active MarketShoppingCart UI detected!");
                    hasLoggedInitialScan = true;
                }

                // Check if already injected
                Transform existing = shoppingCart.transform.Find("FillRackStockButton");
                if (existing != null) return;

                // Find optimal parent container in MarketShoppingCart panel
                Transform parentTransform = shoppingCart.transform;
                Transform footer = shoppingCart.transform.Find("Footer") ?? 
                                  shoppingCart.transform.Find("ActionButtons") ?? 
                                  shoppingCart.transform.Find("Buttons");
                if (footer != null)
                {
                    parentTransform = footer;
                }

                // Construct UI button container
                GameObject btnObj = new GameObject("FillRackStockButton");
                btnObj.transform.SetParent(parentTransform, false);

                RectTransform rt = btnObj.AddComponent<RectTransform>();
                // Compact 150x36 dimensions
                rt.sizeDelta = new Vector2(150f, 36f);

                // Dock near bottom center of cart panel
                rt.anchorMin = new Vector2(0.5f, 0.05f);
                rt.anchorMax = new Vector2(0.5f, 0.05f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, 0f);

                // Ensure button is at the top of z-order raycast stack
                btnObj.transform.SetAsLastSibling();

                // Dark emerald background image
                Image img = btnObj.AddComponent<Image>();
                img.color = new Color(0.08f, 0.40f, 0.20f, 1f);
                img.raycastTarget = true; // Crucial for click registration

                // Button interactivity & states
                Button btn = btnObj.AddComponent<Button>();
                ColorBlock cb = btn.colors;
                cb.normalColor = new Color(0.08f, 0.40f, 0.20f, 1f);
                cb.highlightedColor = new Color(0.12f, 0.55f, 0.28f, 1f);
                cb.pressedColor = new Color(0.05f, 0.28f, 0.14f, 1f);
                cb.disabledColor = new Color(0.30f, 0.30f, 0.30f, 0.5f);
                btn.colors = cb;
                btn.targetGraphic = img;

                // Fix IL2CPP delegate binding using DelegateSupport
                UnityAction clickAction = DelegateSupport.ConvertDelegate<UnityAction>(new Action(OnFillButtonClick));
                btn.onClick.AddListener(clickAction);

                // Text GameObject child
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(btnObj.transform, false);

                RectTransform textRt = textObj.AddComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.sizeDelta = Vector2.zero;

                Text txt = textObj.AddComponent<Text>();
                txt.text = "FILL RACK STOCK";
                txt.fontSize = 13;
                txt.fontStyle = FontStyle.Bold;
                txt.color = Color.white;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.raycastTarget = false; // Prevent child text from blocking clicks

                Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (font != null)
                {
                    txt.font = font;
                }

                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogInfo("[TEST SUCCESS] Successfully injected compact 'FILL RACK STOCK' button with active raycastTarget!");
                }
            }
            catch (Exception ex)
            {
                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogError("[RestockUIComponent] Error injecting UI button: " + ex);
                }
            }
        }

        private static void OnFillButtonClick()
        {
            if (Plugin.LogSource != null)
            {
                Plugin.LogSource.LogInfo("[TEST SUCCESS] FILL RACK STOCK button clicked! Executing auto-restock calculation...");
            }
            RestockCalculator.ExecuteRestockOrder();
        }
    }
}
