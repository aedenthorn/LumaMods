using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;

namespace CraftFromContainers
{
    [BepInPlugin("aedenthorn.CraftFromContainers", "Craft From Containers", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static bool skip;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static BepInExPlugin context;
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



        [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetCount), new Type[] { typeof(InventoryItem) })]
        public static class Inventory_GetCount_Patch1
        {

            public static void Postfix(Inventory __instance, InventoryItem type, ref int __result)
            {
                if (skip || !modEnabled.Value || __instance.Player?.CurrentLevel == null)
                    return;

                IEnumerable<StorageBox> list;
                try
                {
                    list = __instance.Player.CurrentLevel.FindObjectsOfType<StorageBox>().Where(s => s != null && s.IsLocked == false);
                    if (!list.Any())
                        return;
                }
                catch
                {
                    return;
                }

                foreach (var s in list)
                {
                    skip = true;
                    __result += s.Inventory.GetCount(type);
                    skip = false;
                }
            }
        }


        [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetCount), new Type[] { typeof(InventoryItemsData) })]
        public static class Inventory_GetCount_Patch2
        {

            public static void Postfix(Inventory __instance, InventoryItemsData type, ref int __result)
            {
                if (skip || !modEnabled.Value || __instance.Player?.CurrentLevel == null)
                    return;

                IEnumerable<StorageBox> list;
                try
                {
                    list = __instance.Player.CurrentLevel.FindObjectsOfType<StorageBox>().Where(s => s != null && s.IsLocked == false);
                    if (!list.Any())
                        return;
                }
                catch
                {
                    return;
                }

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

                IEnumerable<StorageBox> list;
                try
                {
                    list = __instance.Player.CurrentLevel.FindObjectsOfType<StorageBox>().Where(s => s != null && s.IsLocked == false);
                    if (!list.Any())
                        return;
                }
                catch
                {
                    return;
                }

                foreach (var s in list)
                {
                    skip = true;
                    __result += s.Inventory.GetAmount(itemType);
                    skip = false;
                }
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
                IEnumerable<StorageBox> list;
                try
                {
                    list = __instance.Player.CurrentLevel.FindObjectsOfType<StorageBox>().Where(s => s != null && s.IsLocked == false);
                    if (!list.Any())
                        return true;
                }
                catch 
                {
                    return true;
                }

                for (int i = req.Count - 1; i >= 0; i--)
                {
                    if(req[i]?.item != null)
                    {
                        skip = true;
                        int player = __instance.Player.Inventory.Take(new List<ItemStack> { req[i] });
                        skip = false;
                        req[i].amount -= player;
                        if (req[i].amount > 0)
                        {
                            foreach (var s in list)
                            {
                                skip = true;
                                int storage = s.Inventory.Take(new List<ItemStack> { req[i] });
                                skip = false;
                                req[i].amount -= storage;
                                if (req[i].amount <= 0)
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

                IEnumerable<StorageBox> list;
                try
                {
                    list = __instance.Player.CurrentLevel.FindObjectsOfType<StorageBox>().Where(s => s != null && s.IsLocked == false);
                    if (!list.Any())
                        return;
                }
                catch
                {
                    return;
                }

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
