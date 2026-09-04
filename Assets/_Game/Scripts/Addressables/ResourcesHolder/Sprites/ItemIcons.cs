using System;
using Design.Ids;

namespace AssetsHolder
{
    [Serializable]
    public class ItemIcons : SpriteData
    {
        #if UNITY_EDITOR
        protected override string[] GetAllIds()
        {
            return ItemId.All;
        }
        #endif
    }
}
