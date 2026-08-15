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
                                Plugin.LogSource.LogInfo("[CLICK DETECTED] Red cart logo button clicked! Executing restock...");
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

                // Locate the true primary Shopping Cart button/icon
                GameObject targetCartIconObj = null;
                Image targetCartImg = null;

                // Scan all buttons in MarketShoppingCart and parent canvas
                Button[] buttons = shoppingCart.GetComponentsInChildren<Button>(true);
                if (buttons == null || buttons.Length == 0)
                {
                    if (shoppingCart.transform.parent != null)
                    {
                        buttons = shoppingCart.transform.parent.GetComponentsInChildren<Button>(true);
                    }
                }

                if (buttons != null)
                {
                    foreach (Button btnComp in buttons)
                    {
                        if (btnComp == null) continue;
                        string bName = btnComp.gameObject.name.ToLower();

                        // SKIP ALL CLOSE / EXIT / CANCEL BUTTONS
                        if (bName.Contains("close") || bName.Contains("exit") || bName.Contains("cancel") || bName == "x" || bName.Contains("cross"))
                        {
                            continue;
                        }

                        // Match Shopping Cart button specifically
                        if (bName.Contains("cart") || bName.Contains("basket") || bName.Contains("market"))
                        {
                            if (btnComp.gameObject.name != "FillRackStockButton")
                            {
                                targetCartIconObj = btnComp.gameObject;
                                targetCartImg = btnComp.GetComponent<Image>() ?? btnComp.GetComponentInChildren<Image>();
                                break;
                            }
                        }
                    }
                }

                // If no button matched by name, scan images for cart/basket sprite
                if (targetCartIconObj == null)
                {
                    Image[] images = shoppingCart.GetComponentsInChildren<Image>(true);
                    if (images != null)
                    {
                        foreach (Image imgComp in images)
                        {
                            if (imgComp == null || imgComp.sprite == null) continue;
                            string iName = imgComp.gameObject.name.ToLower();
                            string sName = imgComp.sprite.name != null ? imgComp.sprite.name.ToLower() : "";

                            if (iName.Contains("close") || iName.Contains("exit") || sName.Contains("close") || sName.Contains("exit"))
                            {
                                continue;
                            }

                            if (iName.Contains("cart") || iName.Contains("basket") || sName.Contains("cart") || sName.Contains("basket"))
                            {
                                if (imgComp.gameObject.name != "FillRackStockButton")
                                {
                                    targetCartIconObj = imgComp.gameObject;
                                    targetCartImg = imgComp;
                                    break;
                                }
                            }
                        }
                    }
                }

                // Parent container selection
                Transform parentContainer = shoppingCart.transform;
                if (targetCartIconObj != null && targetCartIconObj.transform.parent != null)
                {
                    parentContainer = targetCartIconObj.transform.parent;
                }

                // Construct Red Cart Logo Button
                GameObject btnObj = new GameObject("FillRackStockButton");
                btnObj.transform.SetParent(parentContainer, false);

                RectTransform rt = btnObj.AddComponent<RectTransform>();
                createdButtonRect = rt;
                createdButtonObj = btnObj;

                if (targetCartIconObj != null)
                {
                    RectTransform targetRt = targetCartIconObj.GetComponent<RectTransform>();
                    if (targetRt != null)
                    {
                        rt.sizeDelta = targetRt.sizeDelta;
                        rt.anchorMin = targetRt.anchorMin;
                        rt.anchorMax = targetRt.anchorMax;
                        rt.pivot = targetRt.pivot;

                        // Position horizontally adjacent to target cart icon (-size.x - 6px spacing)
                        float width = targetRt.sizeDelta.x > 0 ? targetRt.sizeDelta.x : 28f;
                        rt.anchoredPosition = new Vector2(targetRt.anchoredPosition.x - (width + 6f), targetRt.anchoredPosition.y);
                    }

                    // Insert next to target cart icon in sibling index for layout groups
                    int siblingIdx = targetCartIconObj.transform.GetSiblingIndex();
                    btnObj.transform.SetSiblingIndex(siblingIdx);
                }
                else
                {
                    // Fallback top-right header alignment (28x28px)
                    rt.sizeDelta = new Vector2(28f, 28f);
                    rt.anchorMin = new Vector2(1f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-55f, -12f);
                    btnObj.transform.SetAsLastSibling();
                }

                // Bright Red Image Component (#EF4444)
                Image btnImg = btnObj.AddComponent<Image>();
                if (targetCartImg != null && targetCartImg.sprite != null)
                {
                    btnImg.sprite = targetCartImg.sprite;
                }
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

                // Event listener
                UnityAction clickAction = DelegateSupport.ConvertDelegate<UnityAction>(new Action(OnFillButtonClick));
                btn.onClick.AddListener(clickAction);

                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogInfo("[TEST SUCCESS] Successfully cloned primary Shopping Cart button & injected Red Cart Logo button!");
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
                Plugin.LogSource.LogInfo("[EXECUTE RESTOCK] Red Cart Logo button clicked! Populating cart...");
            }
            RestockCalculator.ExecuteRestockOrder();
        }
    }
}
