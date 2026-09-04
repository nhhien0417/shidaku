using System;
using System.Collections.Generic;
using Design.Ids;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AssetsHolder
{
    [Serializable]
    public class ShopItemIcons : SpriteData
    {
        [SerializeField] private List<SpecialIdMapping> _specialIdMappings = new ();

        public bool HasSpecialIdMapping(string id, out string mappedId)
        {
            mappedId = "";

            foreach (var mapping in _specialIdMappings)
            {
                if (mapping.ShopId == id)
                {
                    mappedId = mapping.MappedId;
                    return true;
                }
            }
            return false;
        }

        #if UNITY_EDITOR
        protected override string[] GetAllIds()
        {
            return ShopItemId.All;
        }
        #endif

        [Serializable]
        public struct SpecialIdMapping
        {
            [ValueDropdown("@$root.GetAllIds()")]
            public string ShopId;
            public string MappedId;
        }
    }
}
