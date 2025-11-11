using System;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace AdvancedSort
{
    internal class AdvancedItemStackComparer : IComparer<ItemStack>
    {
        private BepInExPlugin.SortType sortType;
        private bool ascending;

        private readonly Func<ItemStack, bool> m_isItemInvalid;

        public AdvancedItemStackComparer(Func<ItemStack, bool> isItemInvalid, BepInExPlugin.SortType sortType, bool ascending)
        {
            this.sortType = sortType;
            this.ascending = ascending;
            m_isItemInvalid = isItemInvalid;
        }

        public int Compare(ItemStack x, ItemStack y)
        {
            if (x == y)
            {
                return 0;
            }
            if (x?.item == null || x.amount == 0)
            {
                return 1;
            }
            if (y?.item == null || y.amount == 0)
            {
                return -1;
            }

            if (m_isItemInvalid != null)
            {
                int num = (m_isItemInvalid(x) ? 1 : 0);
                int num2 = (m_isItemInvalid(y) ? 1 : 0);
                if (num != num2)
                {
                    return num.CompareTo(num2);
                }
            }

            int result = 0;

            switch (sortType)
            {
                case BepInExPlugin.SortType.Name:
                    result = SortByName(x, y);
                    if(result == 0)
                        result = SortByType(x, y);
                    break;
                case BepInExPlugin.SortType.Type:
                    result = SortByType(x, y);
                    if (result == 0)
                        result = SortByName(x, y);
                    break;
                case BepInExPlugin.SortType.Usage:
                    result = SortByUsage(x, y);
                    if (result == 0)
                        result = SortByName(x, y);
                    break;
                case BepInExPlugin.SortType.Value:
                    result = SortByValue(x, y);
                    if (result == 0)
                        result = SortByName(x, y);
                    break;
            }

            if (result == 0)
            {
                result = -1 * x.amount.CompareTo(y.amount);
            }
            return result;
        }

        private int SortByName(ItemStack x, ItemStack y)
        {
            var result = x.item.GetDescriptiveName().CompareTo(y.item.GetDescriptiveName());
            return result * (ascending ? 1 : -1);
        }

        private int SortByValue(ItemStack x, ItemStack y)
        {
            var result = x.item.type.GetSellValue().CompareTo(y.item.type.GetSellValue());
            return result * (ascending ? 1 : -1);
        }

        private int SortByUsage(ItemStack x, ItemStack y)
        {
            int result = 0;
            if (x.item.type == null && y.item.type == null)
                result = 0;
            else if (x.item.type == null)
                result = -1;
            else if (y.item.type == null)
                result = 1;
            else
            {
                var xl = new LocalizedString("GUI", "comma-list")
                {
                    Arguments = new List<object> { x.item.type.GetLocalizedUses() }
                };
                var xs = xl.GetLocalizedString();
                var yl = new LocalizedString("GUI", "comma-list")
                {
                    Arguments = new List<object> { y.item.type.GetLocalizedUses() }
                };
                var ys = yl.GetLocalizedString();
                result = xs.CompareTo(ys);
                BepInExPlugin.Dbgl($"result: {result}, x: {xs}, y: {ys}");
            }
            return result * (ascending ? 1 : -1);
        }

        private int SortByType(ItemStack x, ItemStack y)
        {
            int result = 0;
            if (x.item.type == null && y.item.type == null)
                result = 0;
            else if (x.item.type == null)
                result = -1;
            else if (y.item.type == null)
                result = 1;
            else
            {
                var xs = x.item.type.GetClassificationName();
                var ys = y.item.type.GetClassificationName();

                result = xs.CompareTo(ys);
                BepInExPlugin.Dbgl($"result: {result}, x: {xs}, y: {ys}");
            }
            return result * (ascending ? 1 : -1);
        }
    }
}