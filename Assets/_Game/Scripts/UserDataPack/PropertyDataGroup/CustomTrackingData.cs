using System;
using System.Collections.Generic;
using Design;
using Design.Structures;
using UnityEngine;
using UserDataPack.Structures;

namespace UserDataPack.PropertyDataGroup
{
    [Serializable]
    public class CustomTrackingData : IUserDataPropertyDataGroup
    {
        public void FixData()
        {
            if (ShopItemBoughtTimesData == null)
            {
                ShopItemBoughtTimesData = new();
            }

            if (IapShopItemBoughtTimesData == null)
            {
                IapShopItemBoughtTimesData = new();
            }

            if (SubscriptionBoughtTimesData == null)
            {
                SubscriptionBoughtTimesData = new();
            }

            if (string.IsNullOrEmpty(LastDailyBoughtTimesDate))
            {
                LastDailyBoughtTimesDate = "";
            }
            if (DailyBoughtTimesData == null)
            {
                DailyBoughtTimesData = new();
            }
            if (DailyBoughtTimesSlotData == null)
            {
                DailyBoughtTimesSlotData = new();
            }

            if (LastDailyBadgeDismissedDates == null)
            {
                LastDailyBadgeDismissedDates = new();
            }

            if (NewIntCollectionItems == null)
            {
                NewIntCollectionItems = new();
            }

            if (NewPromotionOfferIds == null)
            {
                NewPromotionOfferIds = new();
            }
        }

        #region ItemBoughtTimes

        public List<KeyValue<int>> ShopItemBoughtTimesData = new();
        public List<KeyValue<int>> IapShopItemBoughtTimesData = new();
        public List<KeyValue<int>> SubscriptionBoughtTimesData = new();

        public void AddShopItemBoughtTimes(ShopItem shopItem)
        {
            if (shopItem == null)
                return;

            if (shopItem is IapShopItem)
            {
                AddIapShopItemBoughtTimes(shopItem.Id);
            }
            else
            {
                AddShopItemBoughtTimes(shopItem.Id);
            }

            AddDailyBoughtTimes(shopItem.Id);
        }

        public int GetShopItemBoughtTimes(ShopItem shopItem)
        {
            if (shopItem == null)
                return 0;

            if (shopItem is IapShopItem)
            {
                return GetIapShopItemBoughtTimes(shopItem.Id);
            }
            else
            {
                return GetShopItemBoughtTimes(shopItem.Id);
            }
        }

        public void AddShopItemBoughtTimes(string shopItemId, int boughtTimes = 1)
        {
            AddBoughtTimes(ShopItemBoughtTimesData, shopItemId, boughtTimes);
        }

        public int GetShopItemBoughtTimes(string shopItemId)
        {
            return GetBoughtTimes(ShopItemBoughtTimesData, shopItemId);
        }

        public void AddIapShopItemBoughtTimes(string shopItemId, int boughtTimes = 1)
        {
            AddBoughtTimes(IapShopItemBoughtTimesData, shopItemId, boughtTimes);
        }

        public int GetIapShopItemBoughtTimes(string shopItemId)
        {
            return GetBoughtTimes(IapShopItemBoughtTimesData, shopItemId);
        }

        public void AddSubscriptionBoughtTimes(string subscriptionId, int boughtTimes = 1)
        {
            AddBoughtTimes(SubscriptionBoughtTimesData, subscriptionId, boughtTimes);
        }

        public int GetSubscriptionBoughtTimes(string subscriptionId)
        {
            return GetBoughtTimes(SubscriptionBoughtTimesData, subscriptionId);
        }

        private void AddBoughtTimes(List<KeyValue<int>> list, string shopItemId, int boughtTimes)
        {
            var item = list.Find(data => data.Key == shopItemId);
            if (item == null)
            {
                item = new()
                {
                    Key = shopItemId,
                    Value = boughtTimes,
                };
                list.Add(item);
            }
            else
            {
                item.Value += boughtTimes;
            }
        }

        private int GetBoughtTimes(List<KeyValue<int>> list, string shopItemId)
        {
            return list.Find(data => data.Key == shopItemId)?.Value ?? 0;
        }

        public bool HasBoughtIap()
        {
            return IapShopItemBoughtTimesData.Exists(data => data.Value > 0)
                   || SubscriptionBoughtTimesData.Exists(data => data.Value > 0);
        }

        public int GetTotalIapBoughtTimes()
        {
            var total = 0;
            foreach (var it in IapShopItemBoughtTimesData)
            {
                total += it.Value;
            }

            foreach (var it in SubscriptionBoughtTimesData)
            {
                total += it.Value;
            }

            return total;
        }

        #endregion

        #region Daily Bought Times
        public string LastDailyBoughtTimesDate = "";
        public List<KeyValue<int>> DailyBoughtTimesData = new();
        public List<KeyValue<string>> DailyBoughtTimesSlotData = new();

        public void AddDailyBoughtTimes(string shopItemId, int boughtTimes = 1)
        {
            UpdateDailyBoughtTimesSlot(shopItemId);
            AddBoughtTimes(DailyBoughtTimesData, shopItemId, boughtTimes);
        }

        public int GetDailyBoughtTimes(string shopItemId)
        {
            UpdateDailyBoughtTimesSlot(shopItemId);
            return GetBoughtTimes(DailyBoughtTimesData, shopItemId);
        }

        private void UpdateDailyBoughtTimesSlot(string shopItemId)
        {
            if (!DateTimeManager.IsUpToDate)
                return;

            var refreshTimesPerDay = DesignDataHolder.Instance.NormalShopItemData.GetRefreshTimesPerDay(shopItemId);
            if (refreshTimesPerDay <= 0)
                return;

            var slotKey = GetRefreshSlotKey(refreshTimesPerDay);
            var slotEntry = DailyBoughtTimesSlotData.Find(data => data.Key == shopItemId);
            if (slotEntry == null)
            {
                DailyBoughtTimesSlotData.Add(new() { Key = shopItemId, Value = slotKey });
            }
            else if (slotEntry.Value != slotKey)
            {
                slotEntry.Value = slotKey;
                var boughtEntry = DailyBoughtTimesData.Find(data => data.Key == shopItemId);
                if (boughtEntry != null)
                {
                    boughtEntry.Value = 0;
                }
            }
        }

        private static string GetRefreshSlotKey(int refreshTimesPerDay)
        {
            var slotsPerDay = Mathf.Clamp(refreshTimesPerDay, 1, 24);
            var now = DateTimeManager.Now;
            var slotIndex = (int)(now.TimeOfDay.TotalHours / (24.0 / slotsPerDay));
            return $"{now.Date.ToTicksAsString()}#{slotsPerDay}#{slotIndex}";
        }

        #endregion

        #region Daily Badge Dismissed

        public List<KeyValue<string>> LastDailyBadgeDismissedDates = new();

        public void DismissDailyBadge(string badgeKey)
        {
            if (!DateTimeManager.IsUpToDate)
                return;

            var today = DateTimeManager.Now.Date.ToTicksAsString();
            var item = LastDailyBadgeDismissedDates.Find(data => data.Key == badgeKey);
            if (item == null)
            {
                item = new()
                {
                    Key = badgeKey,
                    Value = today,
                };
                LastDailyBadgeDismissedDates.Add(item);
            }
            else
            {
                item.Value = today;
            }
        }

        public bool IsDailyBadgeDismissedToday(string badgeKey)
        {
            if (!DateTimeManager.IsUpToDate)
                return true; // If we can't verify time, don't show badge

            var today = DateTimeManager.Now.Date.ToTicksAsString();
            var item = LastDailyBadgeDismissedDates.Find(data => data.Key == badgeKey);
            return item != null && item.Value == today;
        }

        #endregion

        #region New Collection Items
        public List<KeyValues<int>> NewIntCollectionItems = new();

        public void AddNewCollectionItem(CollectionItem item)
        {
            switch (item)
            {
                case SingleIdCollectionItem<int> singleIdCollectionItem:
                    AddNewIntCollectionItem(singleIdCollectionItem);
                    break;

                default:
                    Debug.LogError($"Collection item type {item.GetType()} not supported.");
                    break;
            }
        }

        private void AddNewIntCollectionItem(SingleIdCollectionItem<int> item)
        {
            var data = NewIntCollectionItems.Find(d => d.Key == item.CollectionType);
            if (data == null)
            {
                data = new KeyValues<int>()
                {
                    Key = item.CollectionType,
                    Values = new List<int>() { item.CollectionId },
                };
                NewIntCollectionItems.Add(data);
            }
            else
            {
                if (!data.Values.Contains(item.CollectionId))
                {
                    data.Values.Add(item.CollectionId);
                }
            }
        }

        public bool RemoveNewCollectionItem(CollectionItem item)
        {
            switch (item)
            {
                case SingleIdCollectionItem<int> singleIdCollectionItem:
                    return RemoveNewIntCollectionItem(singleIdCollectionItem);

                default:
                    Debug.LogError($"Collection item type {item.GetType()} not supported.");
                    break;
            }

            return false;
        }

        private bool RemoveNewIntCollectionItem(SingleIdCollectionItem<int> item)
        {
            var data = NewIntCollectionItems.Find(d => d.Key == item.CollectionType);
            if (data != null)
            {
                return data.Values.Remove(item.CollectionId);
            }

            return false;
        }

        public bool IsNewCollectionItem(CollectionItem item)
        {
            switch (item)
            {
                case SingleIdCollectionItem<int> singleIdCollectionItem:
                    return IsNewIntCollectionItem(singleIdCollectionItem);
            }

            return false;
        }

        private bool IsNewIntCollectionItem(SingleIdCollectionItem<int> item)
        {
            var data = NewIntCollectionItems.Find(d => d.Key == item.CollectionType);
            return data != null && data.Values.Contains(item.CollectionId);
        }
        #endregion

        #region New Promotion Offer
        public List<string> NewPromotionOfferIds = new();

        public bool AddNewPromotionOfferId(string id)
        {
            if (NewPromotionOfferIds.Contains(id))
                return false;
            NewPromotionOfferIds.Add(id);
            return true;
        }

        public bool RemoveNewPromotionOfferId(string id)
        {
            return NewPromotionOfferIds.Remove(id);
        }

        public bool IsNewPromotionOfferId(string id)
        {
            return NewPromotionOfferIds.Contains(id);
        }
        #endregion

        #region User Profile Badge Dismissed
        public bool UserProfileChangeUserNameBadgeDismissed;
        #endregion

        #region OpenDate

        public long FirstOpenDate;
        public long LastOpenDate;

        #endregion
    }
}
