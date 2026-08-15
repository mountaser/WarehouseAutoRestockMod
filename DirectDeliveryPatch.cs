using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace WarehouseRestockMod
{
    [HarmonyPatch(typeof(DeliveryManager), "Delivery", new Type[] { typeof(CartData) })]
    public static class DirectDeliveryPatch
    {
        public static bool Prefix(DeliveryManager __instance, CartData cartData)
        {
            if (ModConfig.DirectToWarehouse == null || !ModConfig.DirectToWarehouse.Value)
            {
                return true;
            }

            if (cartData == null || cartData.ProductInCarts == null || cartData.ProductInCarts.Count == 0)
            {
                return true;
            }

            if (Plugin.LogSource != null)
            {
                Plugin.LogSource.LogInfo("Intercepting Delivery for Direct-to-Warehouse placement...");
            }

            Rack[] racks = GameObject.FindObjectsOfType<Rack>();
            bool allPlaced = true;
            int totalPlaced = 0;

            foreach (ItemQuantity item in cartData.ProductInCarts)
            {
                if (item == null) continue;
                int productID = GetProductIDFromItemQuantity(item);
                int countNeeded = GetCountFromItemQuantity(item);

                if (productID <= 0 || countNeeded <= 0) continue;

                for (int i = 0; i < countNeeded; i++)
                {
                    bool placed = TryPlaceBoxOnRack(racks, productID);
                    if (placed)
                    {
                        totalPlaced++;
                    }
                    else
                    {
                        allPlaced = false;
                        if (Plugin.LogSource != null)
                        {
                            Plugin.LogSource.LogWarning("Rack slots full for ProductID " + productID + " during direct delivery.");
                        }
                    }
                }
            }

            if (Plugin.LogSource != null)
            {
                Plugin.LogSource.LogInfo("Direct-to-Warehouse delivery completed: placed " + totalPlaced + " boxes directly on warehouse racks!");
            }

            // If all ordered boxes were placed directly onto racks, skip vanilla street box spawning
            if (allPlaced)
            {
                return false;
            }

            return true;
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

        private static bool TryPlaceBoxOnRack(Rack[] racks, int productID)
        {
            if (racks == null) return false;

            foreach (Rack rack in racks)
            {
                if (rack == null || rack.RackSlots == null) continue;

                foreach (RackSlot slot in rack.RackSlots)
                {
                    if (slot == null || slot.Data == null || slot.Data.ProductID != productID) continue;

                    int currentBoxes = (slot.Data.RackedBoxDatas != null) ? slot.Data.RackedBoxDatas.Count : 0;
                    int maxBoxes = 2; // Default max boxes per slot

                    if (currentBoxes < maxBoxes)
                    {
                        BoxData newBoxData = new BoxData();
                        newBoxData.ProductID = productID;
                        newBoxData.IsOpen = false;
                        newBoxData.ProductCount = 24;

                        if (slot.Data.RackedBoxDatas != null)
                        {
                            slot.Data.RackedBoxDatas.Add(newBoxData);
                        }

                        try
                        {
                            slot.UpdateRackedBoxDatas();
                            slot.RePositionBoxes();
                        }
                        catch (Exception ex)
                        {
                            if (Plugin.LogSource != null)
                            {
                                Plugin.LogSource.LogWarning("RackSlot update notice: " + ex.Message);
                            }
                        }

                        return true;
                    }
                }
            }

            return false;
        }
    }
}
