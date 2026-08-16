using BepInEx.Configuration;
using UnityEngine;

namespace WarehouseRestockMod
{
    public static class ModConfig
    {
        public static ConfigEntry<bool> DirectToWarehouse;
        public static ConfigEntry<bool> OverrideMaxCartLimit;
        public static ConfigEntry<bool> CapToAvailableCash;
        public static ConfigEntry<bool> ClearCartBeforeFilling;

        public static ConfigEntry<bool> AllowOrderingAfter9PM;

        public static ConfigEntry<bool> DropBoxWhenRackFull;

        public static ConfigEntry<int> WholesaleRestockDiscountPercent;

        public static ConfigEntry<int> CustomDiscountPercentage;
        public static ConfigEntry<bool> AutoDiscountOverstock;

        public static ConfigEntry<bool> ShowDiscountIndicatorsInUI;

        public static ConfigEntry<KeyCode> RestockHotkey;
        public static ConfigEntry<KeyCode> NightOrderingToggleHotkey;
        public static ConfigEntry<KeyCode> AutoDiscountHotkey;

        public static void Initialize(ConfigFile config)
        {
            DirectToWarehouse = config.Bind("General", "DirectToWarehouse", true, "Deliver ordered boxes directly onto assigned warehouse rack slots");
            OverrideMaxCartLimit = config.Bind("General", "OverrideMaxCartLimit", true, "Allow shopping cart capacity to exceed vanilla limit");
            CapToAvailableCash = config.Bind("General", "CapToAvailableCash", true, "Cap restocking orders to available cash balance");
            ClearCartBeforeFilling = config.Bind("General", "ClearCartBeforeFilling", true, "Clear existing cart items before filling missing rack stock");

            AllowOrderingAfter9PM = config.Bind("NightOrdering", "AllowOrderingAfter9PM", true, "Allow placing market app orders and instant delivery after 9:00 PM when market closes");

            DropBoxWhenRackFull = config.Bind("Restockers", "DropBoxWhenRackFull", true, "When a clerk has been standing still for 10+ seconds while still carrying a box (e.g. its target rack slot filled up), make it drop the box and look for new work instead of blocking. Detected by polling, so it reacts within ~12 seconds.");

            WholesaleRestockDiscountPercent = config.Bind("Wholesale", "WholesaleRestockDiscountPercent", 20, "Discount percentage off market box price when restocking via +FILL (0% to 50%)");

            CustomDiscountPercentage = config.Bind("Discounts", "CustomDiscountPercentage", 15, "Custom discount percentage to apply to products (1% to 90%)");
            AutoDiscountOverstock = config.Bind("Discounts", "AutoDiscountOverstock", false, "Automatically apply discount to overstocked items in warehouse");

            ShowDiscountIndicatorsInUI = config.Bind("UI", "ShowDiscountIndicatorsInUI", true, "Display discount badges and strikethrough prices in cart & pricing UI");

            RestockHotkey = config.Bind("Hotkeys", "RestockHotkey", KeyCode.None, "Hotkey to trigger auto-restock calculation (+FILL). Set to None to disable keyboard shortcut.");
            NightOrderingToggleHotkey = config.Bind("Hotkeys", "NightOrderingToggleHotkey", KeyCode.None, "Hotkey to toggle late-night market ordering after 9:00 PM. Set to None to disable keyboard shortcut.");
            AutoDiscountHotkey = config.Bind("Hotkeys", "AutoDiscountHotkey", KeyCode.None, "Hotkey to apply custom discounts on overstocked products. Set to None to disable keyboard shortcut.");
        }
    }
}
