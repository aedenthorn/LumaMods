using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.InputSystem;

namespace QuickTorch
{
    [BepInPlugin("aedenthorn.QuickTorch", "Quick Torch", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static bool skip;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<string> placeKey;
        public static InputAction placeAction;
        public static BepInExPlugin context;

        public static bool listDirty = false;
        
        public static IEnumerable<StorageBox> storageList = new List<StorageBox>();
        public static Level lastLevel;
        public static void Dbgl(object str, LogLevel level = LogLevel.Debug)
        {
            if (isDebug.Value)
                context.Logger.Log(level, str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug log");
            placeKey = Config.Bind<string>("General", "PlaceKey", "<Keyboard>r", "Place key");

            placeAction = new InputAction(binding: placeKey.Value);
            placeAction.Enable();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MetadataHelper.GetMetadata(this).GUID );

        }

        [HarmonyPatch(typeof(LocalPlayerController), nameof(LocalPlayerController.Update))]
        public static class LocalPlayerController_Update_Patch
        {

            public static void Postfix(LocalPlayerController __instance)
            {
                if (!modEnabled.Value)
                    return;
                if (placeAction.WasPressedThisFrame())
                {
                    Dbgl("Pressed place key");
                    for (int i = 0; i < __instance.Inventory.NumSlots; i++)
                    {
                        ItemStack itemStack = __instance.Inventory.m_items[i];

                        if (itemStack?.item?.type?.IsTorch() == true)
                        {
                            InventoryItem item = itemStack.item;
                            itemStack.amount--;
                            if (__instance.Inventory.AutoDeleteEmptyStacks && itemStack.amount == 0)
                            {
                                __instance.Inventory.m_items[i] = null;
                            }
                            __instance.Inventory.SlotChanged(i, -1, item.type, true);
                            Action<ItemStack, int> onRemovedFromInventory = __instance.Inventory.OnRemovedFromInventory;
                            if (onRemovedFromInventory != null)
                            {
                                onRemovedFromInventory(itemStack, 1);
                            }
                            WorldItemsData worldItemsData = item.type.ToWorldItem();

                            __instance.BeginPlacement(worldItemsData, item.type);
                            break;
                        }
                    }
                }
            }
        }
    }
}
