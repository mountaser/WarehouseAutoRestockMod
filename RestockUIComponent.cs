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
                // 1. Hotkey F6 trigger anytime in-game
                if (Input.GetKeyDown(KeyCode.F6))
                {
                    if (Plugin.LogSource != null)
                    {
                        Plugin.LogSource.LogInfo("F6 Hotkey pressed! Executing auto-restock calculation...");
                    }
                    RestockCalculator.ExecuteRestockOrder();
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

                // 3. Visibility Check (Only active when Products/Market Shopping Cart is open)
                CartManager cart = CartManager.Instance;
                MarketShoppingCart shoppingCart = (cart != null) ? cart.MarketShoppingCart : GameObject.FindObjectOfType<MarketShoppingCart>();
                bool isMarketOpen = (shoppingCart != null && shoppingCart.gameObject.activeInHierarchy);

                if (createdButtonObj != null)
                {
                    if (createdButtonObj.activeSelf != isMarketOpen)
                    {
                        createdButtonObj.SetActive(isMarketOpen);
                    }
                }

                // 4. Check UI injection every 0.2 seconds
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

                // CLEANUP & DEDUPLICATION: Search globally for existing FillRackStockButton
                GameObject existingObj = GameObject.Find("FillRackStockButton");
                if (existingObj != null)
                {
                    createdButtonObj = existingObj;
                    createdButtonRect = existingObj.GetComponent<RectTransform>();
                    existingObj.SetActive(true);
                    return;
                }

                // Locate Vehicles button or Cart button in Market App top bar
                GameObject vehicleBtnObj = null;
                GameObject cartLogoObj = null;

                Button[] allButtons = shoppingCart.transform.root.GetComponentsInChildren<Button>(true);
                if (allButtons != null)
                {
                    foreach (Button b in allButtons)
                    {
                        if (b == null) continue;
                        string bName = b.gameObject.name.ToLower();
                        if (bName.Contains("vehicle"))
                        {
                            vehicleBtnObj = b.gameObject;
                        }
                        else if (bName.Contains("cart") || bName.Contains("basket"))
                        {
                            if (b.gameObject.name != "FillRackStockButton")
                            {
                                cartLogoObj = b.gameObject;
                            }
                        }
                    }
                }

                // Select target anchor button (Vehicles button preferred, fallback to Cart logo)
                GameObject anchorObj = vehicleBtnObj ?? cartLogoObj;

                // Select parent container
                Transform parentContainer = shoppingCart.transform;
                if (anchorObj != null && anchorObj.transform.parent != null)
                {
                    parentContainer = anchorObj.transform.parent;
                }

                // Construct Red Restock Button ONCE
                GameObject btnObj = new GameObject("FillRackStockButton");
                btnObj.transform.SetParent(parentContainer, false);

                RectTransform rt = btnObj.AddComponent<RectTransform>();
                createdButtonRect = rt;
                createdButtonObj = btnObj;

                if (anchorObj != null)
                {
                    RectTransform anchorRt = anchorObj.GetComponent<RectTransform>();
                    if (anchorRt != null)
                    {
                        // Match compact logo height/size (28x28px)
                        float iconSize = Math.Min(28f, anchorRt.sizeDelta.y > 0 ? anchorRt.sizeDelta.y : 28f);
                        rt.sizeDelta = new Vector2(iconSize, iconSize);
                        rt.anchorMin = anchorRt.anchorMin;
                        rt.anchorMax = anchorRt.anchorMax;
                        rt.pivot = anchorRt.pivot;

                        // Position horizontally AFTER Vehicles logo (to the right of Vehicles)
                        float width = anchorRt.sizeDelta.x > 0 ? anchorRt.sizeDelta.x : iconSize;
                        rt.anchoredPosition = new Vector2(anchorRt.anchoredPosition.x + (width + 6f), anchorRt.anchoredPosition.y);
                    }

                    // Insert AFTER Vehicles button in sibling layout order
                    int siblingIdx = anchorObj.transform.GetSiblingIndex();
                    btnObj.transform.SetSiblingIndex(siblingIdx + 1);
                }
                else
                {
                    // Fallback top-right header alignment (28x28px pill badge)
                    rt.sizeDelta = new Vector2(28f, 28f);
                    rt.anchorMin = new Vector2(1f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-60f, -12f);
                    btnObj.transform.SetAsLastSibling();
                }

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
                txt.fontSize = 9;
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
                    Plugin.LogSource.LogInfo("[TEST SUCCESS] Placed compact Red Restock button after Vehicles logo on Products tab!");
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
