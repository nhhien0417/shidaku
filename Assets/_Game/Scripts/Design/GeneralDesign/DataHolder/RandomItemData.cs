using System;
using System.Collections.Generic;
using Design.Structures;
using UnityEngine;

namespace Design.DataHolder
{
    [CreateAssetMenu(fileName = "RandomItemData", menuName = "Design/RandomItemData")]
    [Serializable]
    public class RandomItemData : ScriptableObject
    {
        [SerializeReference] private List<RandomItemDefined> _item = new();

        public RandomItemDefined Get(string id)
        {
            return _item?.Find(x => x.GetId() == id);
        }
    }
}