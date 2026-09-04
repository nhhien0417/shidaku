using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Design.Ids;
using Sirenix.OdinInspector;
using Design.Conditions;
using UnityEngine;

namespace Design.Structures
{
    [Serializable]
    public class ShopItem
    {
        [ValueDropdown("GetAllShopItemIds")]
        public string Id;
        public string Name;
        public string IconId;
        public Price Price;
        public int PurchaseLimit;
        public int DailyPurchaseLimit;
        [SerializeReference]
        public List<Item> ItemsReward = new();

        [ValueDropdown("GetAllCouponTypes")]
        public List<string> CouponTypeAllowed = new();
        public List<string> UnlockConditions = new();

        public bool HasPurchaseLimit => PurchaseLimit > 0;
        public bool HasDailyPurchaseLimit => DailyPurchaseLimit > 0;
        public bool HasNoPurchaseLimit => !HasPurchaseLimit && !HasDailyPurchaseLimit;
        public string DisplayIconId => string.IsNullOrEmpty(IconId) ? Id : IconId;

        public bool HasItem(string itemId, out Item item)
        {
            item = ItemsReward.Find(i => i.Id == itemId);
            return item != null;
        }

        public bool IsUnlocked()
        {
            if (UnlockConditions is not { Count: > 0 })
                return true;

            foreach (var condition in UnlockConditions)
            {
                if (condition.ConditionsIsValidAndMet())
                    return true;
            }

            return false;
        }

        #region Analytics
        [IgnoreDataMember] public string Placement { get; set; } // This is used for analytics to identify where the purchase happens. It should be set before purchase and can be reset after purchase.
        [IgnoreDataMember] public string ButtonName { get; set; } // This is used for analytics to identify which button is clicked for purchase. It should be set before purchase and can be reset after purchase.
        [IgnoreDataMember] public string AdReason { get; set; } // This is used for analytics to identify which ad reason is used for purchase. It should be set before purchase and can be reset after purchase.

        public string GetAnalyticsItemName()
        {
            return $"{Id}/{Name}";
        }

        public string GetAnalyticsRewardIds()
        {
            return ItemsReward?.GetAnalyticsRewardIds() ?? "";
        }

        public int GetAnalyticsValue()
        {
            return ItemsReward?.GetAnalyticsRewardValue() ?? 0;
        }

        public virtual string GetAnalyticsBuyActionId()
        {
            return $"buy:{Id}";
        }
        #endregion

        #if UNITY_EDITOR
        private string[] GetAllShopItemIds()
        {
            return ShopItemId.All;
        }

        private string[] GetAllCouponTypes()
        {
            return CouponType.All;
        }
        #endif
    }
}
