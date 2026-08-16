using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace WarehouseRestockMod
{
    [HarmonyPatch(typeof(DeliveryManager), "Delivery", new Type[] { typeof(CartData) })]
    public static class DirectDeliveryPatch
    {
        // Placing many boxes synchronously in one frame overwhelms whatever pool/limit backs
        // BoxGenerator.SpawnBoxInRack - a 141-box order in one go failed 936/936 attempts, while
        // small orders (single digits to ~20) mostly succeeded. So instead of processing the whole
        // delivery in one loop, we queue the boxes and drain a few per frame from
        // RestockUIComponent.Update() via ProcessQueueBatch().
        private static readonly Queue<Box> pendingBoxes = new Queue<Box>();
        private static int batchTotal = 0;
        private static int batchSuccess = 0;
        private const int BoxesPerFrame = 3;

        // Racks don't move/get created mid-delivery, so we scan the scene once per order
        // instead of every frame - GameObject.FindObjectsOfType<Rack>() every frame while
        // draining a 500+ box queue over ~190 frames is what was causing the post-order lag.
        private static Rack[] cachedRacks = null;

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
                Box[] allBoxes = GameObject.FindObjectsOfType<Box>(true);
                if (allBoxes == null || allBoxes.Length == 0) return;

                int queued = 0;
                foreach (Box box in allBoxes)
                {
                    if (box == null || box.Data == null) continue;
                    int productID = box.Data.ProductID;
                    if (productID <= 0) continue;
                    if (IsBoxRacked(box.Data)) continue;

                    pendingBoxes.Enqueue(box);
                    queued++;
                }

                batchTotal += queued;

                if (queued > 0)
                {
                    cachedRacks = GameObject.FindObjectsOfType<Rack>();
                }

                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogInfo(
                        "Post-delivery: queued " + queued + " boxes for gradual direct-to-warehouse placement (" +
                        BoxesPerFrame + "/frame, " + pendingBoxes.Count + " total pending).");
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

        // Called every frame from RestockUIComponent.Update(). Drains a small, fixed number of
        // queued boxes per frame instead of dumping the whole order in one synchronous loop.
        public static void ProcessQueueBatch()
        {
            if (pendingBoxes.Count == 0) return;

            try
            {
                if (cachedRacks == null || cachedRacks.Length == 0)
                {
                    cachedRacks = GameObject.FindObjectsOfType<Rack>();
                }
                if (cachedRacks == null || cachedRacks.Length == 0) return;

                int processed = 0;
                while (processed < BoxesPerFrame && pendingBoxes.Count > 0)
                {
                    Box box = pendingBoxes.Dequeue();
                    processed++;

                    if (box == null || box.Data == null) continue;
                    int productID = box.Data.ProductID;
                    if (productID <= 0) continue;
                    if (IsBoxRacked(box.Data)) continue;

                    bool placed = TryDockBoxToRack(cachedRacks, box, productID);
                    if (placed) batchSuccess++;
                }

                if (pendingBoxes.Count == 0 && batchTotal > 0)
                {
                    if (Plugin.LogSource != null)
                    {
                        Plugin.LogSource.LogInfo(
                            "Direct-to-Warehouse gradual delivery completed! Successfully auto-docked " +
                            batchSuccess + "/" + batchTotal + " boxes onto warehouse racks.");
                    }
                    batchTotal = 0;
                    batchSuccess = 0;
                    cachedRacks = null;
                }
            }
            catch (Exception ex)
            {
                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogError("Error in DirectDeliveryPatch.ProcessQueueBatch: " + ex.ToString());
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

                    // Slots with no physical label placed reliably NRE inside RackSlot.Initialize -
                    // skip them so delivery keeps looking for a slot that can actually take the box.
                    bool hasLabel = true;
                    try { hasLabel = slot.HasLabel; } catch { }
                    if (!hasLabel) continue;

                    int currentBoxes = (slot.Data.RackedBoxDatas != null) ? slot.Data.RackedBoxDatas.Count : 0;
                    int maxBoxes = 2; // Default max box capacity per slot

                    if (currentBoxes < maxBoxes)
                    {
                        // RackSlot.AddBox() throws NRE almost every call here, and
                        // BoxGenerator.SpawnBoxInRack(slot.InteractionPosition, ...) never actually
                        // docks anything either (0/936 and 0/567 across two full sessions) - likely
                        // because InteractionPosition/Rotation are the player's stand-point for using
                        // the slot, not a box placement anchor. RackSlot.Initialize(Box) is what
                        // actually works reliably: it's the exact call the vanilla save loader makes
                        // for every box, and this mod already recorded real successful deliveries
                        // through it (13, 6, 12 boxes in one session). It doesn't reliably add a 2nd
                        // box to an already-occupied slot (silently no-ops instead of throwing), but
                        // the count-verification below catches that safely and just tries another slot.
                        int countBefore = currentBoxes;
                        try
                        {
                            slot.Initialize(box);

                            try { slot.RePositionBoxes(); } catch { }
                            try { slot.UpdateRackedBoxDatas(); } catch { }

                            int countAfter = (slot.Data.RackedBoxDatas != null) ? slot.Data.RackedBoxDatas.Count : 0;
                            if (countAfter > countBefore)
                            {
                                return true;
                            }

                            if (Plugin.LogSource != null)
                            {
                                Plugin.LogSource.LogWarning(
                                    "RackSlot.Initialize() did not actually add the box (count stayed at " + countAfter + "); trying another slot.");
                            }
                        }
                        catch (Exception ex)
                        {
                            // Roll back any partial mutation the failed call may have made so
                            // RackedBoxDatas can't drift ahead of what's actually on the rack -
                            // that drift is what makes the Fill Rack Stock button think a slot
                            // is full when no box is visually there.
                            if (slot.Data != null && slot.Data.RackedBoxDatas != null)
                            {
                                while (slot.Data.RackedBoxDatas.Count > countBefore)
                                {
                                    slot.Data.RackedBoxDatas.RemoveAt(slot.Data.RackedBoxDatas.Count - 1);
                                }
                            }

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
