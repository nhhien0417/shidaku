using System;
using System.Collections.Generic;
using Design.Structures;
using UnityEngine;

namespace Design.DataHolder
{
    [CreateAssetMenu(fileName = "CustomizeShopItemData", menuName = "Design/CustomizeShopItemData", order = 1)]
    [Serializable]
    public class CustomizeShopItemData : ScriptableObject
    {
        [SerializeReference] public List<ShopItem> CustomizeQueens = new();
        [SerializeReference] public List<ShopItem> CustomizeXs = new();

        public List<ShopItem> GetAllCustomizeItems()
        {
            var allItems = new List<ShopItem>();
            if (CustomizeQueens != null)
                allItems.AddRange(CustomizeQueens);
            if (CustomizeXs != null)
                allItems.AddRange(CustomizeXs);
            return allItems;
        }
    }
}