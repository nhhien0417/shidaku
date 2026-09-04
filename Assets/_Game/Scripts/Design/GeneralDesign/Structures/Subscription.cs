using System;
using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace Design.Structures
{
    [Serializable]
    public class Subscription
    {
        public string Id;
        public int RewardIntervalDays;
        public IapProductId WeeklyProductId;
        public IapProductId MonthlyProductId;
        public ProductType ProductType;
        public List<Item> ItemsReward = new();
        
        public string GetTrackingId()
        {
            return $"subscription";
        }
    }
}