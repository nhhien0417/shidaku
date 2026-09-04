using System;
using System.Collections.Generic;
using UnityEngine;
using Design.Structures;

namespace Design.DataHolder
{
    [CreateAssetMenu(fileName = "ItemPriceData", menuName = "Design/ItemPriceData")]
    [Serializable]
    public class ItemPriceData : ScriptableObject
    {
        [SerializeField] private List<ItemPrice> _itemPrices = new ();

        public ItemPrice Get(string id)
        {
            return _itemPrices?.Find(x => x.Id == id);
        }
    }
}