using System;
using System.Collections.Generic;
using Design.Ids;
using UnityEngine;

namespace Design.Structures
{
    [Serializable]
    public class FortuneWheelGift2 : RandomItemDefined
    {
        [SerializeField] private List<RandomEntry> _entries = new();

        public FortuneWheelGift2()
        {
            Id = ItemId.FortuneWheelGift2;
        }

        public override List<Item> GetRandomItems() => PickWeighted(_entries);
    }
}
