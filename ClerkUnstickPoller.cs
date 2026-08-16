using System;
using System.Collections.Generic;
using UnityEngine;
using SupermarketSimulator.Clerk;

namespace WarehouseRestockMod
{
    // The plain global-namespace `Restocker` class (used throughout this file's earlier
    // history) turned out to be a legacy/preview-only type - GameObject.FindObjectsOfType,
    // even with includeInactive: true, only ever found ONE instance in the whole scene
    // (EmployeeId -1, frozen at a fixed near-origin position: the hiring-UI preview model),
    // never any of the actual hired workers. Real hired workers are driven by
    // SupermarketSimulator.Clerk.Clerk instead, confirmed via reflection against
    // BepInEx/interop/Assembly-CSharp.dll - it has the same shape (CarryingBox, EmployeeId,
    // DropBoxToGround()) plus RestartStateMachine() to make it immediately look for new work.
    //
    // Harmony patching Clerk's own methods (GoToWaiting-equivalent state transitions, Unity
    // lifecycle methods, etc.) was not attempted here after Restocker's own methods proved
    // completely un-patchable in this game/BepInEx setup (confirmed via extensive diagnostic
    // instrumentation - not even OnEnable/Start ever fired for a type with otherwise-working
    // instances). Instead this polls for the observable SYMPTOM: a clerk that is
    // CarryingBox == true and hasn't moved in a while is stuck. Direct property reads and
    // direct method calls (not Harmony-patched interception) are used throughout, since those
    // are proven reliable elsewhere in this mod. Called periodically from
    // RestockUIComponent.Update(), which is proven to tick reliably every frame.
    public static class ClerkUnstickPoller
    {
        private const float PollIntervalSeconds = 2f;
        private const float StuckThresholdSeconds = 10f;
        private const float MovementThreshold = 0.3f; // meters; below this counts as "not moved"

        private class Tracked
        {
            public Vector3 lastPosition;
            public float lastMovedTime;
        }

        private static readonly Dictionary<int, Tracked> tracked = new Dictionary<int, Tracked>();
        private static float lastPollTime = 0f;
        private static bool loggedReadFailure = false;

        public static void Poll()
        {
            if (Time.time - lastPollTime < PollIntervalSeconds) return;
            lastPollTime = Time.time;

            if (ModConfig.DropBoxWhenRackFull == null || !ModConfig.DropBoxWhenRackFull.Value)
            {
                tracked.Clear();
                return;
            }

            try
            {
                Clerk[] clerks = GameObject.FindObjectsOfType<Clerk>(true);
                if (clerks == null || clerks.Length == 0) return;

                HashSet<int> seenIds = new HashSet<int>();

                foreach (Clerk c in clerks)
                {
                    if (c == null) continue;
                    if (!c.gameObject.activeInHierarchy) continue;

                    int id;
                    bool carrying;
                    Vector3 pos;
                    try
                    {
                        id = c.EmployeeId;
                        carrying = c.CarryingBox;
                        pos = c.transform.position;
                    }
                    catch
                    {
                        if (!loggedReadFailure)
                        {
                            loggedReadFailure = true;
                            if (Plugin.LogSource != null)
                            {
                                Plugin.LogSource.LogWarning(
                                    "ClerkUnstickPoller: failed to read EmployeeId/CarryingBox/position off a Clerk instance - " +
                                    "this member may have been renamed or removed in the current game build. Skipping affected clerks silently from now on.");
                            }
                        }
                        continue;
                    }

                    // Same sentinel pattern as the legacy Restocker preview instance (EmployeeId -1):
                    // skip any clerk with a negative EmployeeId as defense in depth.
                    if (id < 0) continue;

                    if (!carrying)
                    {
                        tracked.Remove(id);
                        continue;
                    }

                    seenIds.Add(id);
                    float now = Time.time;

                    if (!tracked.TryGetValue(id, out Tracked t))
                    {
                        tracked[id] = new Tracked { lastPosition = pos, lastMovedTime = now };
                        continue;
                    }

                    float moved = Vector3.Distance(t.lastPosition, pos);
                    if (moved > MovementThreshold)
                    {
                        t.lastPosition = pos;
                        t.lastMovedTime = now;
                        continue;
                    }

                    if (now - t.lastMovedTime >= StuckThresholdSeconds)
                    {
                        if (Plugin.LogSource != null)
                        {
                            Plugin.LogSource.LogInfo(
                                "Clerk " + id + " has been stationary for " + (now - t.lastMovedTime).ToString("F1") +
                                "s while carrying a box - treating as stuck, dropping it instead of blocking.");
                        }

                        try
                        {
                            c.DropBoxToGround();
                            c.RestartStateMachine();
                        }
                        catch (Exception ex)
                        {
                            if (Plugin.LogSource != null)
                            {
                                Plugin.LogSource.LogWarning("Error unsticking Clerk " + id + ": " + ex.ToString());
                            }
                        }

                        tracked.Remove(id);
                    }
                }

                // Drop tracking for any clerk that disappeared (despawned/destroyed) since last poll.
                if (tracked.Count > seenIds.Count)
                {
                    List<int> stale = new List<int>();
                    foreach (KeyValuePair<int, Tracked> kvp in tracked)
                    {
                        if (!seenIds.Contains(kvp.Key)) stale.Add(kvp.Key);
                    }
                    foreach (int staleId in stale) tracked.Remove(staleId);
                }
            }
            catch (Exception ex)
            {
                if (Plugin.LogSource != null)
                {
                    Plugin.LogSource.LogError("Error in ClerkUnstickPoller.Poll: " + ex.ToString());
                }
            }
        }
    }
}
