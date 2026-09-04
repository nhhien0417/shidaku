using System;
using System.Collections.Generic;
using Design.Ids;
using UnityEngine;

namespace Design.Structures
{
    [Serializable]
    public class FortuneWheelGift1 : RandomItemDefined
    {
        [SerializeField] private List<RandomEntry> _entries = new();

        public FortuneWheelGift1()
        {
            Id = ItemId.FortuneWheelGift1;
        }

        public override List<Item> GetRandomItems() => PickWeighted(_entries);
    }
}
