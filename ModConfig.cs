using BepInEx.Configuration;

namespace WarehouseRestockMod
{
    public static class ModConfig
    {
        public static ConfigEntry<bool> DirectToWarehouse;
        public static ConfigEntry<bool> OverrideMaxCartLimit;
        public static ConfigEntry<bool> CapToAvailableCash;
        public static ConfigEntry<bool> ClearCartBeforeFilling;

        public static void Initialize(ConfigFile config)
        {
            DirectToWarehouse = config.Bind("General", "DirectToWarehouse", true, "Deliver ordered boxes directly onto assigned warehouse rack slots");
            OverrideMaxCartLimit = config.Bind("General", "OverrideMaxCartLimit", true, "Allow shopping cart capacity to exceed vanilla limit");
            CapToAvailableCash = config.Bind("General", "CapToAvailableCash", true, "Cap restocking orders to available cash balance");
            ClearCartBeforeFilling = config.Bind("General", "ClearCartBeforeFilling", true, "Clear existing cart items before filling missing rack stock");
        }
    }
}
