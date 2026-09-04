using System;

namespace UserDataPack.Structures.CustomTrackingData
{
    [Serializable]
    public class ShopItemBoughtTimes
    {
        public string ShopItemId;
        public int Times;
        public bool IsInAppPurchase;
    }

    [Serializable]
    public class SubscriptionBoughtTimes
    {
        public string SubscriptionId;
        public int Times;
    }
}