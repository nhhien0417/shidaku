using UnityEngine;
using System;
using Sirenix.OdinInspector;
using Design.Ids;
using Unity.VisualScripting;
using System.Collections.Generic;

namespace Design.Structures
{
    [Serializable]
    public class Item
    {
        [ValueDropdown("GetAllItemIds")]
        public string Id;

        public int Amount;

#if UNITY_EDITOR
        protected virtual string[] GetAllItemIds()
        {
            return ItemId.All;
        }
#endif
    }

    public static class ItemUtils
    {
        public static string GetAnalyticsRewardIds(this List<Item> items)
        {
            if (items == null || items.Count == 0)
                return "";
            return string.Join(", ", items.ConvertAll(r => $"{r.Id}:{r.Amount}"));
        }

        public static int GetAnalyticsRewardValue(this List<Item> items)
        {
            var result = 0;
            foreach (var item in items)
            {
                result += item.Amount;
            }
            return result;
        }

        public static List<Item> ParseToItems(this string data, string itemSeparator = ",", string amountSeparator = ":")
        {
            var result = new List<Item>();

            if (string.IsNullOrEmpty(data))
                return result;

            var items = data.Split(itemSeparator);
            foreach (var item in items)
            {
                var parts = item.Split(amountSeparator);
                if (parts.Length != 2)
                {
                    Debug.LogError($"Invalid item string: {item}");
                    continue;
                }

                var id = parts[0];
                if (!int.TryParse(parts[1], out var amount))
                {
                    Debug.LogError($"Invalid amount for item: {item}");
                    continue;
                }

                Item itemData = null;
                if (ItemId.IsCollectionItem(id))
                {
                    switch (id)
                    {
                        case ItemId.Avatar:
                        case ItemId.AvatarFrame:
                        case ItemId.ProfileBanner:
                        case ItemId.CustomizeQueen:
                        case ItemId.CustomizeX:
                            itemData = new SingleIdCollectionItem<int> { CollectionId = amount, CollectionType = id };
                            break;
                    }
                }
                else
                {
                    itemData = new Item { Id = id, Amount = amount };
                }

                result.Add(itemData);
            }

            return result;
        }
    }
}