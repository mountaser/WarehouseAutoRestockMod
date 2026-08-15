using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarehouseRestockMod
{
    public static class DiscountPatch
    {
        public static void ApplyDiscountToProduct(int productID, int percentage)
        {
            if (PriceManager.Instance == null) return;

            Pricing pricingData = PriceManager.Instance.GetPriceSetByPlayer(productID);
            if (pricingData == null)
            {
                pricingData = PriceManager.Instance.GetPrice(productID);
            }

            if (pricingData != null)
            {
                int clampPercent = Mathf.Clamp(percentage, 0, 90);
                pricingData.DiscountRate = clampPercent;

                PriceManager.Instance.PriceSet(pricingData);

                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogInfo("Applied " + clampPercent + "% discount to ProductID " + productID + ". Price: $" + pricingData.Price.ToString("F2"));
                }
            }
        }

        public static void ToggleOverstockDiscounts()
        {
            int customPercent = (ModConfig.CustomDiscountPercentage != null) ? ModConfig.CustomDiscountPercentage.Value : 15;

            Rack[] racks = GameObject.FindObjectsOfType<Rack>();
            if (racks == null) return;

            Dictionary<int, int> productBoxCounts = new Dictionary<int, int>();

            foreach (Rack rack in racks)
            {
                if (rack == null || rack.RackSlots == null) continue;
                foreach (RackSlot slot in rack.RackSlots)
                {
                    if (slot == null || slot.Data == null) continue;
                    int productID = slot.Data.ProductID;
                    if (productID <= 0) continue;

                    int count = (slot.Data.RackedBoxDatas != null) ? slot.Data.RackedBoxDatas.Count : 0;
                    if (productBoxCounts.ContainsKey(productID))
                        productBoxCounts[productID] += count;
                    else
                        productBoxCounts[productID] = count;
                }
            }

            int discountedCount = 0;
            foreach (KeyValuePair<int, int> kvp in productBoxCounts)
            {
                if (kvp.Value >= 3)
                {
                    ApplyDiscountToProduct(kvp.Key, customPercent);
                    discountedCount++;
                }
            }

            if (Plugin.LogSource != null)
            {
                Plugin.LogSource.LogInfo("Overstock discount calculation complete. Applied " + customPercent + "% discount to " + discountedCount + " overstocked products.");
            }
        }
    }
}
