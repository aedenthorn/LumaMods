using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace AutoStore
{
    [BepInPlugin("aedenthorn.AutoStore", "Auto Store", "0.1.0")]
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

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MetadataHelper.GetMetadata(this).GUID );

        }

        [HarmonyPatch(typeof(MagneticLootFactory), nameof(MagneticLootFactory.CreateItems), new Type[] { typeof(Level), typeof(List<ItemStack>), typeof(LevelPosition), typeof(MagneticLootFlags), typeof(MagneticLootCreationSource), typeof(bool) })]
        public static class MagneticLootFactory_CreateItems_Patch
        {

            public static void Prefix(Level level, List<ItemStack> itemStacks, MagneticLootFlags flags)
            {
                if (!modEnabled.Value || flags != MagneticLootFlags.OnTopOfBuilding)
                    return;
                IEnumerable<StorageBox> list;
                try
                {
                    list = level.FindObjectsOfType<StorageBox>().Where(s => s != null && s.IsLocked == false);
                }
                catch
                {
                    return;
                }
                for(int i = itemStacks.Count - 1; i >= 0; i--)
                {
                    var stack = itemStacks[i];
                    if (stack?.item == null)
                        continue;
                    foreach (var s in list)
                    {
                        if (s.Inventory.GetCount(stack.item) > 0 && s.Inventory.Add(stack.item, stack.amount))
                        {
                            Dbgl($"storing {stack.item.GetDescriptiveName()} in storage");
                            itemStacks.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }
    }
}
