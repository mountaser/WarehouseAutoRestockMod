using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace WarehouseRestockMod
{
    [HarmonyPatch(typeof(DeliveryManager), "Delivery", new Type[] { typeof(CartData) })]
    public static class DirectDeliveryPatch
    {
        public static void Postfix(DeliveryManager __instance, CartData cartData)
        {
            if (ModConfig.DirectToWarehouse == null || !ModConfig.DirectToWarehouse.Value)
            {
                return;
            }

            if (cartData == null || cartData.ProductInCarts == null || cartData.ProductInCarts.Count == 0)
            {
                return;
            }

            try
            {
                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogInfo("Post-delivery processing: Moving delivered boxes directly onto warehouse racks...");
                }

                Rack[] racks = GameObject.FindObjectsOfType<Rack>();
                if (racks == null || racks.Length == 0) return;

                Box[] allBoxes = GameObject.FindObjectsOfType<Box>(true);
                if (allBoxes == null || allBoxes.Length == 0) return;

                int movedCount = 0;

                foreach (Box box in allBoxes)
                {
                    if (box == null || box.Data == null) continue;
                    int productID = box.Data.ProductID;
                    if (productID <= 0) continue;

                    bool isRacked = IsBoxRacked(box.Data);
                    if (isRacked) continue;

                    // Try placing this loose box onto a matching open rack slot
                    bool placed = TryDockBoxToRack(racks, box, productID);
                    if (placed)
                    {
                        movedCount++;
                    }
                }

                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogInfo("Direct-to-Warehouse delivery completed! Successfully auto-docked " + movedCount + " boxes onto warehouse racks.");
                }
            }
            catch (Exception ex)
            {
                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogError("Error in DirectDeliveryPatch Postfix: " + ex.ToString());
                }
            }
        }

        private static bool IsBoxRacked(BoxData data)
        {
            if (data == null) return false;
            try
            {
                Type t = data.GetType();
                PropertyInfo pR = t.GetProperty("Racked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (pR != null) return Convert.ToBoolean(pR.GetValue(data, null));

                FieldInfo fR = t.GetField("Racked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fR != null) return Convert.ToBoolean(fR.GetValue(data));
            }
            catch { }
            return false;
        }

        private static bool TryDockBoxToRack(Rack[] racks, Box box, int productID)
        {
            if (racks == null || box == null) return false;

            foreach (Rack rack in racks)
            {
                if (rack == null || rack.RackSlots == null) continue;

                foreach (RackSlot slot in rack.RackSlots)
                {
                    if (slot == null || slot.Data == null || slot.Data.ProductID != productID) continue;

                    int currentBoxes = (slot.Data.RackedBoxDatas != null) ? slot.Data.RackedBoxDatas.Count : 0;
                    int maxBoxes = 2; // Default max box capacity per slot

                    if (currentBoxes < maxBoxes)
                    {
                        try
                        {
                            if (slot.Boxes == null || slot.Boxes.Count == 0)
                            {
                                slot.Initialize(box);
                            }
                            else
                            {
                                slot.AddBox(productID, box, true);
                            }

                            try { slot.RePositionBoxes(); } catch { }
                            try { slot.UpdateRackedBoxDatas(); } catch { }

                            return true;
                        }
                        catch (Exception ex)
                        {
                            if (Plugin.LogSource != null)
                            {
                                Plugin.LogSource.LogWarning("Failed to dock box to rack slot: " + ex.Message);
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
