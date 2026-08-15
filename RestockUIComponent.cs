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

                // 3. Check UI every 0.2 seconds
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
                    createdButtonRect = null;
                    createdButtonObj = null;
                    return;
                }

                if (!hasLoggedInitialScan && Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogInfo("[RestockUIComponent] Active MarketShoppingCart UI detected!");
                    hasLoggedInitialScan = true;
                }

                // Check if already injected in hierarchy
                Transform existing = shoppingCart.transform.Find("FillRackStockButton");
                if (existing == null && shoppingCart.transform.parent != null)
                {
                    existing = shoppingCart.transform.parent.Find("FillRackStockButton");
                }
                if (existing != null)
                {
                    createdButtonObj = existing.gameObject;
                    createdButtonRect = existing.GetComponent<RectTransform>();
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

                // Select target anchor button (Vehicles button or Cart logo)
                GameObject anchorObj = vehicleBtnObj ?? cartLogoObj;

                // Select parent container
                Transform parentContainer = shoppingCart.transform;
                if (anchorObj != null && anchorObj.transform.parent != null)
                {
                    parentContainer = anchorObj.transform.parent;
                }

                // Construct Red Restock Button
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
                        rt.sizeDelta = anchorRt.sizeDelta;
                        rt.anchorMin = anchorRt.anchorMin;
                        rt.anchorMax = anchorRt.anchorMax;
                        rt.pivot = anchorRt.pivot;

                        // Position horizontally immediately to the left of Vehicles button
                        float width = anchorRt.sizeDelta.x > 0 ? anchorRt.sizeDelta.x : 36f;
                        rt.anchoredPosition = new Vector2(anchorRt.anchoredPosition.x - (width + 6f), anchorRt.anchoredPosition.y);
                    }

                    // Insert next to anchor in sibling layout order
                    int siblingIdx = anchorObj.transform.GetSiblingIndex();
                    btnObj.transform.SetSiblingIndex(Math.Max(0, siblingIdx - 1));
                }
                else
                {
                    // Fallback top-right header alignment (80x28px pill badge)
                    rt.sizeDelta = new Vector2(80f, 28f);
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

                // Add crisp uppercase label text (+RESTOCK)
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(btnObj.transform, false);

                RectTransform textRt = textObj.AddComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.sizeDelta = Vector2.zero;

                Text txt = textObj.AddComponent<Text>();
                txt.text = "+RESTOCK";
                txt.fontSize = 10;
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
                    Plugin.LogSource.LogInfo("[TEST SUCCESS] Successfully placed Red Restock button immediately left of Vehicles button!");
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
