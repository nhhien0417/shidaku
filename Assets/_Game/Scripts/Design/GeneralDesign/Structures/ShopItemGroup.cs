using System;
using System.Collections.Generic;
using Design.Conditions;

namespace Design.Structures
{
    [Serializable]
    public class ShopItemGroup
    {
        public string Id;
        public int RefreshTimesPerDay;
        public List<string> UnlockConditions = new();
        public List<ShopItem> ShopItems = new();
        public List<IapShopItem> IapShopItems = new();

        public List<ShopItem> GetAllShopItems()
        {
            var items = new List<ShopItem>(ShopItems);
            items.AddRange(IapShopItems);
            return items;
        }

        public ShopItem FindShopItem(string id)
        {
            return ShopItems.Find(i => i.Id == id) ?? IapShopItems.Find(i => i.Id == id);
        }

        public bool IsUnlocked()
        {
            if (UnlockConditions is not { Count: > 0 })
                return true;

            foreach (var condition in UnlockConditions)
            {
                if (condition.ConditionsIsValidAndMet())
                    return true;
            }

            return false;
        }
    }
}
