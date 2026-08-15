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

                // Check if already injected
                Transform existing = shoppingCart.transform.Find("FillRackStockButton");
                if (existing != null)
                {
                    createdButtonObj = existing.gameObject;
                    createdButtonRect = existing.GetComponent<RectTransform>();
                    return;
                }

                // Attach to shopping cart UI panel root
                GameObject btnObj = new GameObject("FillRackStockButton");
                btnObj.transform.SetParent(shoppingCart.transform, false);

                RectTransform rt = btnObj.AddComponent<RectTransform>();
                createdButtonRect = rt;
                createdButtonObj = btnObj;

                // COMPACT RED CART ICON BADGE (28x28px - exact logo icon size)
                rt.sizeDelta = new Vector2(28f, 28f);

                // Anchor to Top-Right header next to green cart logo
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-45f, -8f);

                // Ensure top z-order in canvas stack
                btnObj.transform.SetAsLastSibling();

                // Look for existing cart sprite to clone
                Sprite cartSprite = null;
                Image[] childImages = shoppingCart.GetComponentsInChildren<Image>(true);
                if (childImages != null)
                {
                    foreach (Image childImg in childImages)
                    {
                        if (childImg != null && childImg.sprite != null && 
                            (childImg.gameObject.name.IndexOf("cart", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             childImg.gameObject.name.IndexOf("icon", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            cartSprite = childImg.sprite;
                            break;
                        }
                    }
                }

                // Bright Red Image Component (#EF4444)
                Image img = btnObj.AddComponent<Image>();
                if (cartSprite != null)
                {
                    img.sprite = cartSprite;
                }
                img.color = new Color(0.94f, 0.27f, 0.27f, 1f); // Bright Red (#EF4444)
                img.raycastTarget = true;

                // Button component
                Button btn = btnObj.AddComponent<Button>();
                ColorBlock cb = btn.colors;
                cb.normalColor = new Color(0.94f, 0.27f, 0.27f, 1f);
                cb.highlightedColor = new Color(1.00f, 0.40f, 0.40f, 1f);
                cb.pressedColor = new Color(0.75f, 0.15f, 0.15f, 1f);
                btn.colors = cb;
                btn.targetGraphic = img;

                // Event listener
                UnityAction clickAction = DelegateSupport.ConvertDelegate<UnityAction>(new Action(OnFillButtonClick));
                btn.onClick.AddListener(clickAction);

                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogInfo("[TEST SUCCESS] Injected 28x28px Red Cart Logo Icon button into Top-Right header!");
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
