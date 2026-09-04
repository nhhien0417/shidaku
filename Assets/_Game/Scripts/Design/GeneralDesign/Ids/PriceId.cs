using System;

namespace Design.Ids
{
    public abstract class PriceId
    {
        public const string Free = "free";
        public const string IAP = "iap";
        public const string Ads = "ads";
        
        public static readonly string[] All =
        {
            Free,
            IAP,
            Ads,
            ItemId.Coin,
        };
    }
}