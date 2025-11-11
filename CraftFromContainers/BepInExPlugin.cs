using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CraftFromContainers
{
    [BepInPlugin("aedenthorn.CraftFromContainers", "Craft From Containers", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static bool skip;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

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

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MetadataHelper.GetMetadata(this).GUID );

        }
        public static IEnumerable<StorageBox> GetStorageList(Level currentLevel, bool force = false)
        {
            if (!modEnabled.Value)
                return new List<StorageBox>();
            if(lastLevel != currentLevel || force || listDirty)
            {
                try
                {
                    storageList = currentLevel.FindObjectsOfType<StorageBox>().Where(s => s != null && s.IsLocked == false);
                }
                catch { }
            }
            lastLevel = currentLevel;
            listDirty = false;
            return storageList;
        }


        [HarmonyPatch(typeof(StorageBox), nameof(StorageBox.OnPlace))]
        public static class StorageBox_OnPlace_Patch
        {

            public static void Postfix(StorageBox __instance)
            {
                if (!modEnabled.Value)
                    return;
                listDirty = true;
            }
        }


        [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetCount), new Type[] { typeof(InventoryItemsData) })]
        public static class Inventory_GetCount_Patch2
        {

            public static void Postfix(Inventory __instance, InventoryItemsData type, ref int __result)
            {
                if (skip || !modEnabled.Value || __instance.Player?.CurrentLevel == null)
                    return;

                IEnumerable<StorageBox> list = GetStorageList(__instance.Player.CurrentLevel);

                foreach (var s in list)
                {
                    skip = true;
                    __result += s.Inventory.GetCount(type);
                    skip = false;
                }
            }
        }


        [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetAmount))]
        public static class Inventory_GetAmount_Patch
        {

            public static void Postfix(Inventory __instance, InventoryItemsData itemType, ref int __result)
            {
                if (skip || !modEnabled.Value || __instance.Player?.CurrentLevel == null)
                    return;

                IEnumerable<StorageBox> list = GetStorageList(__instance.Player.CurrentLevel);

                foreach (var s in list)
                {
                    skip = true;
                    __result += s.Inventory.GetAmount(itemType);
                    skip = false;
                }
            }
        }


        [HarmonyPatch(typeof(Inventory), nameof(Inventory.TakeOne), new Type[] { typeof(InventoryItem), typeof(int), typeof(bool) })]
        public static class Inventory_TakeOne_Patch
        {
            public static bool Prefix(Inventory __instance, InventoryItem item, int preferredIndex, bool showNotification, ref int __result)
            {
                if (skip || !modEnabled.Value || __instance.Player?.CurrentLevel == null)
                    return true;

                var TryTakeOne = AccessTools.Method(typeof(Inventory), "TryTakeOne");
                for (int i = 0; i < __instance.NumSlots; i++)
                {
                    if ((bool)TryTakeOne.Invoke(__instance, new object[] { item, i, showNotification }))
                    {
                        __result = i;
                        return false;
                    }
                }
                IEnumerable<StorageBox> list = GetStorageList(__instance.Player.CurrentLevel);

                foreach (var s in list)
                {
                    for (int i = 0; i < s.Inventory.NumSlots; i++)
                    {
                        if ((bool)TryTakeOne.Invoke(s.Inventory, new object[] { item, i, showNotification }))
                        {
                            __result = i;
                            return false;
                        }
                    }
                }
                Dbgl($"Item {item.GetDescriptiveName()} not found!", LogLevel.Warning);
                __result = -1;
                return false;
            }
        }


        [HarmonyPatch(typeof(Inventory), nameof(Inventory.Take), new Type[] { typeof(List<ItemStack>), typeof(int), typeof(bool), typeof(bool) })]
        public static class Inventory_Take_Patch
        {
            public static bool Prefix(Inventory __instance, List<ItemStack> required, int preferredIndex, bool onlyUsePreferredIndex, bool showNotification, ref int __result)
            {
                if (skip || !modEnabled.Value || __instance.Player?.CurrentLevel == null)
                    return true;

                var req = new List<ItemStack>();
                req.AddRange(required);

                IEnumerable<StorageBox> list = GetStorageList(__instance.Player.CurrentLevel);

                for (int i = req.Count - 1; i >= 0; i--)
                {
                    if(req[i]?.item != null)
                    {
                        int amount = req[i].amount;
                        skip = true;
                        int player = __instance.Take(new List<ItemStack> {
                            new ItemStack
                            {
                                item = req[i].item,
                                amount = req[i].amount
                            } 
                        });
                        skip = false;
                        __result += player;
                        amount -= player;
                        if (amount > 0)
                        {
                            foreach (var s in list)
                            {
                                skip = true;
                                int storage = s.Inventory.Take(new List<ItemStack> {
                                    new ItemStack
                                    {
                                        item = req[i].item,
                                        amount = amount
                                    }
                                });
                                skip = false;
                                amount -= storage;
                                __result += storage;
                                if (amount <= 0)
                                    goto next;
                            }
                        }
                    }
                next:
                    continue;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.Contains), new Type[] { typeof(List<ItemStack>) })]
        public static class Inventory_Contains_Patch
        {
            public static void Postfix(Inventory __instance, List<ItemStack> required, ref bool __result)
            {
                if (skip || !modEnabled.Value || __result || __instance.Player?.CurrentLevel == null)
                    return;
                var req = new List<ItemStack>();
                req.AddRange(required);

                IEnumerable<StorageBox> list = GetStorageList(__instance.Player.CurrentLevel);

                for (int i = req.Count - 1; i >= 0; i--)
                {
                    foreach (var s in list)
                    {
                        skip = true;
                        if (req[i]?.item == null || s.Inventory.Contains(req[i]))
                        {
                            skip = false;
                            req.RemoveAt(i);
                            if(req.Count == 0)
                            {
                                __result = true;
                                return;
                            }
                            goto cont;
                        }
                        skip = false;
                    }
                    return;
                cont:
                    continue;
                }
            }
        }
    }
}
