using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace WarehouseRestockMod
{
    public static class RestockCalculator
    {
        public static void ExecuteRestockOrder()
        {
            if (Plugin.LogSource != null)
            {
                Plugin.LogSource.LogInfo("Executing Fill Rack Stock calculation...");
            }

            CartManager cart = CartManager.Instance;
            if (cart == null)
            {
                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogError("CartManager.Instance is null!");
                }
                return;
            }

            // Clear cart if configured
            if (cart.CartData != null && cart.CartData.ProductInCarts != null)
            {
                if (ModConfig.ClearCartBeforeFilling != null && ModConfig.ClearCartBeforeFilling.Value)
                {
                    cart.CartData.ProductInCarts.Clear();
                    if (Plugin.LogSource != null)
                    {
                        Plugin.LogSource.LogInfo("Cleared existing shopping cart.");
                    }
                }
            }

            Dictionary<int, int> neededBoxes = new Dictionary<int, int>();

            // 1. Scan Warehouse Racks
            Rack[] racks = GameObject.FindObjectsOfType<Rack>();
            if (racks != null)
            {
                foreach (Rack rack in racks)
                {
                    if (rack == null || rack.RackSlots == null) continue;

                    foreach (RackSlot slot in rack.RackSlots)
                    {
                        if (slot == null || slot.Data == null) continue;
                        int productID = slot.Data.ProductID;
                        if (productID <= 0) continue;

                        int currentBoxes = (slot.Data.RackedBoxDatas != null) ? slot.Data.RackedBoxDatas.Count : 0;
                        int maxBoxes = 2; // Default max box capacity per slot

                        int needed = maxBoxes - currentBoxes;
                        if (needed > 0)
                        {
                            if (neededBoxes.ContainsKey(productID))
                            {
                                neededBoxes[productID] += needed;
                            }
                            else
                            {
                                neededBoxes[productID] = needed;
                            }
                        }
                    }
                }
            }

            // 2. Count Loose Boxes in Delivery Landing Area / Floor
            Dictionary<int, int> landingBoxes = new Dictionary<int, int>();
            Box[] looseBoxes = GameObject.FindObjectsOfType<Box>();
            if (looseBoxes != null)
            {
                foreach (Box box in looseBoxes)
                {
                    if (box == null || box.Data == null) continue;
                    int pId = box.Data.ProductID;
                    if (pId <= 0) continue;

                    if (landingBoxes.ContainsKey(pId))
                    {
                        landingBoxes[pId]++;
                    }
                    else
                    {
                        landingBoxes[pId] = 1;
                    }
                }
            }

            // Deduct loose landing area boxes from needed count
            foreach (KeyValuePair<int, int> landingKvp in landingBoxes)
            {
                int pId = landingKvp.Key;
                int countOnFloor = landingKvp.Value;
                if (neededBoxes.ContainsKey(pId))
                {
                    neededBoxes[pId] = Math.Max(0, neededBoxes[pId] - countOnFloor);
                }
            }

            // Deduct items already sitting in cart
            if (cart.CartData != null && cart.CartData.ProductInCarts != null)
            {
                foreach (ItemQuantity itemQty in cart.CartData.ProductInCarts)
                {
                    if (itemQty == null) continue;
                    int pId = GetProductIDFromItemQuantity(itemQty);
                    int qtyInCart = GetCountFromItemQuantity(itemQty);
                    if (pId > 0 && qtyInCart > 0 && neededBoxes.ContainsKey(pId))
                    {
                        neededBoxes[pId] = Math.Max(0, neededBoxes[pId] - qtyInCart);
                    }
                }
            }

            // Filter out 0 needed entries
            Dictionary<int, int> finalOrder = new Dictionary<int, int>();
            foreach (KeyValuePair<int, int> kvp in neededBoxes)
            {
                if (kvp.Value > 0)
                {
                    finalOrder[kvp.Key] = kvp.Value;
                }
            }

            if (finalOrder.Count == 0)
            {
                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogInfo("All warehouse racks are full or pending delivery in landing area!");
                }
                return;
            }

            float availableCash = float.MaxValue;
            if (MoneyManager.Instance != null)
            {
                availableCash = MoneyManager.Instance.Money;
            }

            LocalMarketProductDatabase db = GameObject.FindObjectOfType<LocalMarketProductDatabase>();

            float currentCartTotal = 0f;
            int addedCount = 0;

            foreach (KeyValuePair<int, int> kvp in finalOrder)
            {
                int productID = kvp.Key;
                int countNeeded = kvp.Value;

                float pricePerBox = 10f;
                if (db != null)
                {
                    LocalMarketProductCost costEntry;
                    if (db.GetEntryById(productID, out costEntry))
                    {
                        if (costEntry.Product != null)
                        {
                            pricePerBox = costEntry.Product.BoxPrice;
                        }
                    }
                }

                int boxesToAdd = 0;
                for (int i = 0; i < countNeeded; i++)
                {
                    bool capEnabled = (ModConfig.CapToAvailableCash != null && ModConfig.CapToAvailableCash.Value);
                    if (capEnabled && (currentCartTotal + pricePerBox > availableCash))
                    {
                        if (Plugin.LogSource != null)
                        {
                            Plugin.LogSource.LogInfo("Cash limit reached. Capping cart additions.");
                        }
                        break;
                    }

                    boxesToAdd++;
                    currentCartTotal += pricePerBox;
                    addedCount++;
                }

                if (boxesToAdd > 0 && cart.CartData != null && cart.CartData.ProductInCarts != null)
                {
                    ItemQuantity itemQty = new ItemQuantity(productID, boxesToAdd);
                    cart.CartData.ProductInCarts.Add(itemQty);
                }

                bool capEnabledOuter = (ModConfig.CapToAvailableCash != null && ModConfig.CapToAvailableCash.Value);
                if (capEnabledOuter && (currentCartTotal + pricePerBox > availableCash))
                {
                    break;
                }
            }

            // Trigger cart UI update
            try
            {
                MarketShoppingCart marketCart = cart.MarketShoppingCart ?? GameObject.FindObjectOfType<MarketShoppingCart>();
                if (marketCart != null)
                {
                    marketCart.ReGenerateCartUI();
                }
            }
            catch (Exception ex)
            {
                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogWarning("ReGenerateCartUI notice: " + ex.Message);
                }
            }

            if (Plugin.LogSource != null)
            {
                Plugin.LogSource.LogInfo("Successfully added " + addedCount + " missing boxes for " + finalOrder.Count + " products to cart! Estimated total: $" + currentCartTotal.ToString("F2"));
            }
        }

        private static int GetProductIDFromItemQuantity(ItemQuantity item)
        {
            if (item == null) return 0;
            System.Type t = item.GetType();
            FieldInfo f = t.GetField("First", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null) return System.Convert.ToInt32(f.GetValue(item));
            PropertyInfo p = t.GetProperty("First", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null) return System.Convert.ToInt32(p.GetValue(item, null));
            return 0;
        }

        private static int GetCountFromItemQuantity(ItemQuantity item)
        {
            if (item == null) return 0;
            System.Type t = item.GetType();
            FieldInfo f = t.GetField("Second", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null) return System.Convert.ToInt32(f.GetValue(item));
            PropertyInfo p = t.GetProperty("Second", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null) return System.Convert.ToInt32(p.GetValue(item, null));
            return 0;
        }
    }
}
