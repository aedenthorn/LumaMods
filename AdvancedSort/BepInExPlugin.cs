using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace AdvancedSort
{
    [BepInPlugin("aedenthorn.AdvancedSort", "Advanced Sort", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> sortEmptySlots;
        public static ConfigEntry<bool> currentSortAsc;
        public static ConfigEntry<SortType> currentSort;

        public static BepInExPlugin context;

        public static void Dbgl(object str, LogLevel level = LogLevel.Debug)
        {
            if (isDebug.Value)
                context.Logger.Log(level, str);
        }

        public enum SortType
        {
            Name,
            Type,
            Usage,
            Value
        }

        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug log");
            sortEmptySlots = Config.Bind<bool>("Options", "SortEmptySlots", true, "Sort Empty Slots");
            
            currentSort = Config.Bind<SortType>("ZInternal", "CurrentSort", SortType.Type, "Current Sort Type");
            currentSortAsc = Config.Bind<bool>("ZInternal", "CurrentSortAsc", true, "Current Sort Asc");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MetadataHelper.GetMetadata(this).GUID );

        }
        private static void MakeButtons(ButtonWidget sortButton, bool offset = false)
        {
            bool found = false;
            foreach(Transform c in sortButton.transform.parent)
            {
                if (c.name == sortButton.name + "_asc")
                {
                    var t = c.GetComponentInChildren<TextMeshProUGUI>();
                    t.text = currentSortAsc.Value ? "Asc." : "Desc.";
                    found = true;
                }
                else if (c.name == sortButton.name + "_type")
                {
                    var t = c.GetComponentInChildren<TextMeshProUGUI>();
                    t.text = currentSort.Value.ToString();
                    found = true;
                }
            }
            if (found)
                return;
            var fi = AccessTools.Field(typeof(ButtonWidget), "m_button");
            
            ButtonWidget sortType = Instantiate(sortButton, sortButton.transform.parent);
            sortType.name = sortButton.name + "_type";
            DestroyImmediate(sortType.GetComponentInChildren<LocalizeStringEvent>());
            DestroyImmediate(sortType.GetComponentInChildren<LocalizedFontEvent>());
            var tmp = sortType.GetComponentInChildren<TextMeshProUGUI>();
            tmp.text = currentSort.Value.ToString();
            ((Button)fi.GetValue(sortType)).onClick.AddListener(delegate
            {
                CycleSortType();
                tmp.text = currentSort.Value.ToString();
            });
            if (offset)
            {
                var rt = sortType.GetComponent<RectTransform>();
                rt.anchoredPosition += new Vector2(0, 12.5f);
            }

            ButtonWidget sortAsc = Instantiate(sortButton, sortButton.transform.parent);
            sortAsc.name = sortButton.name + "_asc";
            DestroyImmediate(sortAsc.GetComponentInChildren<LocalizeStringEvent>());
            DestroyImmediate(sortAsc.GetComponentInChildren<LocalizedFontEvent>());
            var tmp2 = sortAsc.GetComponentInChildren<TextMeshProUGUI>();
            tmp2.text = currentSortAsc.Value ? "Asc." : "Desc.";
            ((Button)fi.GetValue(sortAsc)).onClick.AddListener(delegate
            {
                currentSortAsc.Value = !currentSortAsc.Value;
                tmp2.text = currentSortAsc.Value ? "Asc." : "Desc.";
            });
            if (offset)
            {
                var rt = sortAsc.GetComponent<RectTransform>();
                rt.anchoredPosition += new Vector2(50, 12.5f);
            }
        }
        private static void CycleSortType()
        {
            var types = Enum.GetValues(typeof(SortType));
            if (currentSort.Value == (SortType)types.GetValue(types.Length - 1))
                currentSort.Value = (SortType)types.GetValue(0);
            else
            {
                currentSort.Value += 1;
            }
        }

        [HarmonyPatch(typeof(PlayerInventoryPanel), nameof(PlayerInventoryPanel.OnCreate))]
        public static class PlayerInventoryPanel_OnCreate_Patch
        {

            public static void Prefix(PlayerInventoryPanel __instance, ButtonWidget ___m_sortButton)
            {
                if (!modEnabled.Value)
                    return;
                MakeButtons(___m_sortButton);
            }

        }
        [HarmonyPatch(typeof(UIChestWindow), "OnOpened")]
        public static class UIChestWindow_OnOpened_Patch
        {

            public static void Prefix(UIChestWindow __instance, ButtonWidget ___m_sortChestButton, ButtonWidget ___m_sortInventoryButton)
            {
                if (!modEnabled.Value)
                    return;
                MakeButtons(___m_sortChestButton, true);
            }
        }
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.Sort))]
        public static class Inventory_Sort_Patch
        {

            public static void Prefix(Inventory __instance, ref IComparer<ItemStack> order)
            {
                if (modEnabled.Value)
                {
                    order = new AdvancedItemStackComparer((Func<ItemStack, bool>)AccessTools.Field(typeof(ItemStackComparer), "m_isItemInvalid").GetValue(order), currentSort.Value, currentSortAsc.Value);
                }
            }
        }
    }
}
