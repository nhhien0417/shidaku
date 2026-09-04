using System;
using System.Collections.Generic;
using Design.Ids;
using UnityEngine;

namespace Design.Structures
{
    [Serializable]
    public class RandomBoosterItem : RandomItemDefined
    {
        [SerializeField] private List<RandomEntry> _entries = new();

        public RandomBoosterItem()
        {
            Id = ItemId.RandomBooster;
        }

        public override List<Item> GetRandomItems() => PickWeighted(_entries);
    }
}
