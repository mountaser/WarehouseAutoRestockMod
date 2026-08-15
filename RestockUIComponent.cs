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

                // 2. Direct mouse click detection (WorldSpace Camera & Overlay)
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
                if (existing == null)
                {
                    existing = shoppingCart.transform.parent != null ? shoppingCart.transform.parent.Find("FillRackStockButton") : null;
                }
                if (existing != null)
                {
                    createdButtonObj = existing.gameObject;
                    createdButtonRect = existing.GetComponent<RectTransform>();
                    return;
                }

                // Locate cart icon GameObject (specifically excluding close/exit buttons)
                GameObject neighborIcon = null;
                Image neighborImg = null;

                Image[] childImages = shoppingCart.GetComponentsInChildren<Image>(true);
                if (childImages == null || childImages.Length == 0)
                {
                    if (shoppingCart.transform.parent != null)
                    {
                        childImages = shoppingCart.transform.parent.GetComponentsInChildren<Image>(true);
                    }
                }

                if (childImages != null)
                {
                    foreach (Image imgComp in childImages)
                    {
                        if (imgComp == null || imgComp.sprite == null) continue;
                        string objName = imgComp.gameObject.name.ToLower();
                        string spriteName = imgComp.sprite.name != null ? imgComp.sprite.name.ToLower() : "";

                        // EXPLICITLY SKIP CLOSE/EXIT/CANCEL BUTTONS
                        if (objName.Contains("close") || objName.Contains("exit") || objName.Contains("cancel") || objName.Contains("x") ||
                            spriteName.Contains("close") || spriteName.Contains("exit") || spriteName.Contains("cancel"))
                        {
                            continue;
                        }

                        // MATCH CART OR BASKET OR SHOPPING GRAPHIC SPECIFICALLY
                        if (objName.Contains("cart") || objName.Contains("basket") || objName.Contains("shop") ||
                            spriteName.Contains("cart") || spriteName.Contains("basket") || spriteName.Contains("shop"))
                        {
                            if (imgComp.gameObject.name != "FillRackStockButton")
                            {
                                neighborIcon = imgComp.gameObject;
                                neighborImg = imgComp;
                                break;
                            }
                        }
                    }
                }

                // Fallback: If no cart-named image found, pick first non-close image with a sprite
                if (neighborIcon == null && childImages != null)
                {
                    foreach (Image imgComp in childImages)
                    {
                        if (imgComp == null || imgComp.sprite == null) continue;
                        string objName = imgComp.gameObject.name.ToLower();
                        if (objName.Contains("close") || objName.Contains("exit") || objName.Contains("cancel")) continue;
                        if (imgComp.gameObject.name != "FillRackStockButton")
                        {
                            neighborIcon = imgComp.gameObject;
                            neighborImg = imgComp;
                            break;
                        }
                    }
                }

                // Parent container selection
                Transform parentContainer = shoppingCart.transform;
                if (neighborIcon != null && neighborIcon.transform.parent != null)
                {
                    parentContainer = neighborIcon.transform.parent;
                }

                // Construct Red Cart Logo Button
                GameObject btnObj = new GameObject("FillRackStockButton");
                btnObj.transform.SetParent(parentContainer, false);

                RectTransform rt = btnObj.AddComponent<RectTransform>();
                createdButtonRect = rt;
                createdButtonObj = btnObj;

                if (neighborIcon != null)
                {
                    RectTransform neighborRt = neighborIcon.GetComponent<RectTransform>();
                    if (neighborRt != null)
                    {
                        rt.sizeDelta = neighborRt.sizeDelta;
                        rt.anchorMin = neighborRt.anchorMin;
                        rt.anchorMax = neighborRt.anchorMax;
                        rt.pivot = neighborRt.pivot;

                        // Position horizontally adjacent to cart icon
                        float spacing = neighborRt.sizeDelta.x > 0 ? neighborRt.sizeDelta.x + 8f : 32f;
                        rt.anchoredPosition = new Vector2(neighborRt.anchoredPosition.x - spacing, neighborRt.anchoredPosition.y);
                    }

                    // Insert next to cart icon in sibling index for layout groups
                    int siblingIdx = neighborIcon.transform.GetSiblingIndex();
                    btnObj.transform.SetSiblingIndex(siblingIdx);
                }
                else
                {
                    // Fallback top-right header alignment (28x28px)
                    rt.sizeDelta = new Vector2(28f, 28f);
                    rt.anchorMin = new Vector2(1f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-48f, -10f);
                    btnObj.transform.SetAsLastSibling();
                }

                // Bright Red Image Component (#EF4444)
                Image btnImg = btnObj.AddComponent<Image>();
                if (neighborImg != null && neighborImg.sprite != null)
                {
                    btnImg.sprite = neighborImg.sprite;
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
                    Plugin.LogSource.LogInfo("[TEST SUCCESS] Successfully cloned true Cart Icon sprite & injected Red Cart Logo button!");
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
