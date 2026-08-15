using System.Collections.Generic;
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

            Dictionary<int, int> missingBoxes = new Dictionary<int, int>();
            Rack[] racks = GameObject.FindObjectsOfType<Rack>();

            if (racks == null || racks.Length == 0)
            {
                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogWarning("No racks found in scene!");
                }
                return;
            }

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
                        if (missingBoxes.ContainsKey(productID))
                        {
                            missingBoxes[productID] += needed;
                        }
                        else
                        {
                            missingBoxes[productID] = needed;
                        }
                    }
                }
            }

            if (missingBoxes.Count == 0)
            {
                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogInfo("All warehouse racks are already 100% full!");
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

            foreach (KeyValuePair<int, int> kvp in missingBoxes)
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

            if (Plugin.LogSource != null)
            {
                Plugin.LogSource.LogInfo("Successfully added " + addedCount + " missing boxes for " + missingBoxes.Count + " products to cart! Estimated total: $" + currentCartTotal.ToString("F2"));
            }
        }
    }
}
