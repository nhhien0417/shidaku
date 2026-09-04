using System;
using Design.Ids;

namespace AssetsHolder
{
    [Serializable]
    public class CurrencyIcons : SpriteData
    {
        #if UNITY_EDITOR
        protected override string[] GetAllIds()
        {
            return PriceId.All;
        }
        #endif
    }
}
