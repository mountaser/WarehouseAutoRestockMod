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
        private RectTransform createdButtonRect = null;
        private GameObject createdButtonObj = null;

        private void Update()
        {
            try
            {
                // 1. Configurable Hotkeys (Default to None / Unmapped)
                if (ModConfig.RestockHotkey != null && ModConfig.RestockHotkey.Value != KeyCode.None && Input.GetKeyDown(ModConfig.RestockHotkey.Value))
                {
                    if (Plugin.LogSource != null)
                    {
                        Plugin.LogSource.LogInfo("Restock Hotkey pressed! Executing auto-restock calculation...");
                    }
                    RestockCalculator.ExecuteRestockOrder();
                }

                if (ModConfig.NightOrderingToggleHotkey != null && ModConfig.NightOrderingToggleHotkey.Value != KeyCode.None && Input.GetKeyDown(ModConfig.NightOrderingToggleHotkey.Value))
                {
                    if (ModConfig.AllowOrderingAfter9PM != null)
                    {
                        ModConfig.AllowOrderingAfter9PM.Value = !ModConfig.AllowOrderingAfter9PM.Value;
                        if (Plugin.LogSource != null)
                        {
                            Plugin.LogSource.LogInfo("Night Ordering toggled to: " + ModConfig.AllowOrderingAfter9PM.Value);
                        }
                    }
                }

                if (ModConfig.AutoDiscountHotkey != null && ModConfig.AutoDiscountHotkey.Value != KeyCode.None && Input.GetKeyDown(ModConfig.AutoDiscountHotkey.Value))
                {
                    if (Plugin.LogSource != null)
                    {
                        Plugin.LogSource.LogInfo("Auto-Discount Hotkey pressed! Applying discounts to overstocked products...");
                    }
                    DiscountPatch.ToggleOverstockDiscounts();
                }

                // 2. Direct mouse left-click detection (WorldSpace Camera & Overlay)
                if (createdButtonRect != null && createdButtonObj != null && createdButtonObj.activeInHierarchy)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        Canvas canvas = createdButtonObj.GetComponentInParent<Canvas>();
                        Camera worldCam = (canvas != null) ? canvas.worldCamera : null;
                        Camera mainCam = Camera.main;

                        bool isClicked = RectTransformUtility.RectangleContainsScreenPoint(createdButtonRect, Input.mousePosition, worldCam) ||
                                         RectTransformUtility.RectangleContainsScreenPoint(createdButtonRect, Input.mousePosition, mainCam) ||
                                         RectTransformUtility.RectangleContainsScreenPoint(createdButtonRect, Input.mousePosition, null);

                        if (isClicked)
                        {
                            if (Plugin.LogSource != null)
                            {
                                Plugin.LogSource.LogInfo("[CLICK DETECTED] Red Restock button clicked! Executing restock...");
                            }
                            OnFillButtonClick();
                        }
                    }
                }

                // 3. Strict Tab / Panel Visibility Restriction
                CartManager cart = CartManager.Instance;
                MarketShoppingCart shoppingCart = (cart != null) ? cart.MarketShoppingCart : GameObject.FindObjectOfType<MarketShoppingCart>();
                bool isCartPanelOpen = (shoppingCart != null && shoppingCart.gameObject.activeInHierarchy);

                if (createdButtonObj != null)
                {
                    if (createdButtonObj.activeSelf != isCartPanelOpen)
                    {
                        createdButtonObj.SetActive(isCartPanelOpen);
                    }
                }

                // 4. Check UI injection every 0.2 seconds
                if (Time.time - lastCheckTime > 0.2f)
                {
                    lastCheckTime = Time.time;
                    CheckAndInjectUIButton();
                }

                // 5. Drain a few queued direct-to-warehouse box placements this frame
                DirectDeliveryPatch.ProcessQueueBatch();
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

                if (shoppingCart == null || !shoppingCart.gameObject.activeInHierarchy)
                {
                    if (createdButtonObj != null)
                    {
                        createdButtonObj.SetActive(false);
                    }
                    return;
                }

                if (!hasLoggedInitialScan && Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogInfo("[RestockUIComponent] Active MarketShoppingCart UI detected!");
                    hasLoggedInitialScan = true;
                }

                // Continuous Cart UI Price Discount Formatter
                CartItemDiscountUtility.FormatAllCartItems(shoppingCart);

                // CLEANUP & DEDUPLICATION: Search inside MarketShoppingCart for existing button
                Transform existing = shoppingCart.transform.Find("FillRackStockButton");
                if (existing != null)
                {
                    createdButtonObj = existing.gameObject;
                    createdButtonRect = existing.GetComponent<RectTransform>();
                    existing.gameObject.SetActive(true);
                    return;
                }

                // Parent DIRECTLY to MarketShoppingCart panel root (guarantees docking inside cart header)
                GameObject btnObj = new GameObject("FillRackStockButton");
                btnObj.transform.SetParent(shoppingCart.transform, false);

                RectTransform rt = btnObj.AddComponent<RectTransform>();
                createdButtonRect = rt;
                createdButtonObj = btnObj;

                // Compact 26x26px Red Badge
                rt.sizeDelta = new Vector2(26f, 26f);

                // Dock in top-right inner header of MarketShoppingCart panel (matching user's red square)
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-42f, -12f);

                // Elevate to top z-order inside MarketShoppingCart canvas
                btnObj.transform.SetAsLastSibling();

                // Standalone Bright Red Background Image (#EF4444)
                Image btnImg = btnObj.AddComponent<Image>();
                btnImg.color = new Color(0.94f, 0.27f, 0.27f, 1f); // Bright Red (#EF4444)
                btnImg.raycastTarget = true;

                // Button component
                Button btn = btnObj.AddComponent<Button>();
                ColorBlock cb = btn.colors;
                cb.normalColor = new Color(0.94f, 0.27f, 0.27f, 1f);
                cb.highlightedColor = new Color(1.00f, 0.40f, 0.40f, 1f);
                cb.pressedColor = new Color(0.75f, 0.15f, 0.15f, 1f);
                btn.colors = cb;
                btn.targetGraphic = btnImg;

                // Add crisp uppercase label text (+FILL)
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(btnObj.transform, false);

                RectTransform textRt = textObj.AddComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.sizeDelta = Vector2.zero;

                Text txt = textObj.AddComponent<Text>();
                txt.text = "+FILL";
                txt.fontSize = 8;
                txt.fontStyle = FontStyle.Bold;
                txt.color = Color.white;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.raycastTarget = false;

                Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (font != null)
                {
                    txt.font = font;
                }

                // Event listener
                UnityAction clickAction = DelegateSupport.ConvertDelegate<UnityAction>(new Action(OnFillButtonClick));
                btn.onClick.AddListener(clickAction);

                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogInfo("[TEST SUCCESS] Successfully docked Red Restock button inside MarketShoppingCart header!");
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
                Plugin.LogSource.LogInfo("[EXECUTE RESTOCK] Red Restock button clicked! Populating cart...");
            }
            RestockCalculator.ExecuteRestockOrder();
        }
    }
}
