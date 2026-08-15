using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace WarehouseRestockMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        public static ManualLogSource LogSource;
        public static Plugin Instance;

        public override void Load()
        {
            Instance = this;
            LogSource = Log;

            ModConfig.Initialize(Config);

            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

            SafePatch(harmony, typeof(MarketUIPatch));
            SafePatch(harmony, typeof(CartLimitPatch_CartMaxed));
            SafePatch(harmony, typeof(CartLimitPatch_CartMaxedPassive));
            SafePatch(harmony, typeof(DirectDeliveryPatch));

            Log.LogInfo(PluginInfo.PLUGIN_NAME + " v" + PluginInfo.PLUGIN_VERSION + " initialized and loaded successfully!");
        }

        private static void SafePatch(Harmony harmony, Type patchType)
        {
            try
            {
                harmony.CreateClassProcessor(patchType).Patch();
                if (LogSource != null)
                {
                    LogSource.LogInfo("Successfully applied Harmony patch: " + patchType.Name);
                }
            }
            catch (Exception ex)
            {
                if (LogSource != null)
                {
                    LogSource.LogWarning("Failed to apply Harmony patch " + patchType.Name + ": " + ex.Message);
                }
            }
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.opencode.warehouserestock";
        public const string PLUGIN_NAME = "Warehouse Auto-Restock Mod";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}
