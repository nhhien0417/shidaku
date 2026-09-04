using System;
using System.Collections.Generic;
using Design.Structures;
using UnityEngine;

namespace Design.DataHolder
{
    [CreateAssetMenu(fileName = "IapShopItemData", menuName = "Design/IapShopItemData", order = 1)]
    [Serializable]
    public class IapShopItemData : ScriptableObject
    {
        public List<IapShopItem> Items = new ();

        public IEnumerable<IapShopItem> GetAllItems()
        {
            foreach (var item in Items)
                yield return item;

            var shopItemData = DesignDataHolder.Instance?.NormalShopItemData;
            if (shopItemData == null)
                yield break;

            foreach (var shopItem in shopItemData.GetAllShopItems())
            {
                if (shopItem is IapShopItem iapItem)
                    yield return iapItem;
            }
        }

        public IapShopItem GetShopItem(string id)
        {
            foreach (var item in GetAllItems())
            {
                if (item.Id == id)
                    return item;
            }

            return null;
        }

        public IapShopItem GetShopItemHas(string itemId)
        {
            foreach (var item in GetAllItems())
            {
                if (item.ItemsReward.Exists(reward => reward.Id == itemId))
                    return item;
            }

            return null;
        }
    }
}