using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Design.Ids;
using Design.Structures;
using Unity.VisualScripting;
using UnityEngine;
using UserDataPack.PropertyDataGroup;

namespace UserDataPack
{
    [Serializable]
    public class UserData
    {
        #region Load/Save

        private static UserData instance;

        public static UserData Instance
        {
            get
            {
                if (instance == null)
                {

                    instance = UserDataHelper.LoadObscured<UserData>();

                    if (instance == null)
                    {
                        instance = new();
                    }

                    instance.FixData();
                    instance.UpdateData();
                }

                return instance;
            }
        }

        public void Save()
        {
            UserDataHelper.SaveObscured(this);
        }

        private void FixData()
        {
            if (TrackingData == null)
            {
                TrackingData = new();
            }
            TrackingData.FixData();

            if (SecuredData == null)
            {
                SecuredData = new();
            }
            SecuredData.FixData();

            if (SubscriptionData == null)
            {
                SubscriptionData = new();
            }
            SubscriptionData.FixData();

            if (GameplayData == null)
            {
                GameplayData = new();
            }
            GameplayData.FixData();

            if (UserProfile == null)
            {
                UserProfile = new();
            }
            UserProfile.FixData();

            if (CustomizeData == null)
            {
                CustomizeData = new();
            }
            CustomizeData.FixData();

            if (FirstTimeData == null)
            {
                FirstTimeData = new();
            }
            FirstTimeData.FixData();

            if (CollectionData == null)
            {
                CollectionData = new();
            }
            CollectionData.FixData();

            if (PromotionOfferData == null)
            {
                PromotionOfferData = new();
            }
            PromotionOfferData.FixData();

            if (NewVersionMigrateData == null)
            {
                NewVersionMigrateData = new();
            }
            NewVersionMigrateData.FixData();

            if (RandomRateData == null)
            {
                RandomRateData = new();
            }
            RandomRateData.FixData();
        }

        private void UpdateData()
        {
            var nowOnTicks = DateTime.Now.Date.Ticks;

            if (TrackingData.FirstOpenDate <= 0)
            {
                TrackingData.FirstOpenDate = nowOnTicks;
            }

            if (TrackingData.LastOpenDate < nowOnTicks)
            {
                TrackingData.LastOpenDate = nowOnTicks;
            }

            SecuredData.UpdateData();
            PromotionOfferData.UpdateData();
        }

        #endregion

        // Items
        public UserProfile UserProfile = new();
        public CustomizeData CustomizeData = new();
        public SecuredData SecuredData = new();
        public GameplayData GameplayData = new();
        public FirstTimeData FirstTimeData = new();
        public SubscriptionHolder SubscriptionData = new();
        public CollectionData CollectionData = new();
        public PromotionOfferData PromotionOfferData = new();
        public NewVersionMigrateData NewVersionMigrateData = new();
        public RandomRateData RandomRateData = new();

        //Tracking data
        public CustomTrackingData TrackingData = new();

        #region Actions
        [DoNotSerialize][NonSerialized][IgnoreDataMember] public Action<string, int> OnResourceItemChanged;
        [DoNotSerialize][NonSerialized][IgnoreDataMember] public Action<string, int> OnResourceItemSpent;
        [DoNotSerialize][NonSerialized][IgnoreDataMember] public Action<string, int> OnNonconsumableItemChanged;
        #endregion

        #region Item Functions
        public void AddItems(string source, params Item[] items)
        {
            foreach (var item in items)
            {
                if (item is CollectionItem collectionItem)
                {
                    AddCollectionItem(collectionItem, source);
                }
                else
                {
                    AddItem(item.Id, item.Amount, source);
                }
            }
        }

        public void AddItems(List<Item> items, string source)
        {
            foreach (var item in items)
            {
                if (item is CollectionItem collectionItem)
                {
                    AddCollectionItem(collectionItem, source);
                }
                else
                {
                    AddItem(item.Id, item.Amount, source);
                }
            }
        }

        public void AddItem(string itemId, int amount, string source)
        {
            if (ItemId.IsConsumable(itemId))
            {
                AddResourceItem(itemId, amount, source);
            }
            else if (ItemId.IsNonConsumable(itemId))
            {
                AddNonconsumableItem(itemId, amount, source);
            }
            else
            {
                Debug.LogError($"Can not determine item type for itemId: {itemId}");
            }
        }

        public void RemoveItem(string itemId)
        {
            if (ItemId.IsConsumable(itemId))
            {
                SpendResourceItem(itemId, GetResourceItemAmount(itemId), "remove_item");
            }
            else if (ItemId.IsNonConsumable(itemId))
            {
                SecuredData.NonConsumableItems.RemoveAll(x => x.Id == itemId);
            }
            else
            {
                Debug.LogError($"Can not determine item type for itemId: {itemId}");
            }
        }

        public int GetItemAmount(string itemId)
        {
            if (ItemId.IsConsumable(itemId))
                return GetResourceItemAmount(itemId);

            if (ItemId.IsNonConsumable(itemId))
                return GetNonconsumableItemAmount(itemId);

            Debug.LogError($"Can not determine item type for itemId: {itemId}");
            return 0;
        }
        #endregion

        #region ResourceItem Functions
        private void AddResourceItem(string resourceId, int amount, string source)
        {
            if (amount == 0)
                return;

            var resource = SecuredData.AddResourceItem(resourceId, amount);
            if (resource != null)
            {
                UserDataExtensions.TrackResourceItemAdded(resource, amount, source);
                OnResourceItemChanged?.Invoke(resourceId, resource.Amount);
            }
        }

        public int GetResourceItemAmount(string resourceId)
        {
            return SecuredData.GetResourceItemAmount(resourceId);
        }

        public bool SpendResourceItem(string resourceId, int amount, string spentAction = "")
        {
            var result = SecuredData.SpendResourceItem(resourceId, amount, out var resourceItem);
            if (result && resourceItem != null)
            {
                UserDataExtensions.TrackResourceItemSpent(resourceItem, amount, spentAction);
                OnResourceItemChanged?.Invoke(resourceId, resourceItem.Amount);
                OnResourceItemSpent?.Invoke(resourceId, amount);
            }

            return result;
        }

        public bool ResourceItemDataExists(string resourceId)
        {
            return SecuredData.ResourceItemDataExists(resourceId);
        }
        #endregion

        #region Non-Consumable Items Functions
        public int GetNonconsumableItemAmount(string itemId)
        {
            switch (itemId)
            {
                case ItemId.NoAds_24h:
                case ItemId.NoAds_7Days:
                case ItemId.NoAdsLimitedTime:
                    return (int)SecuredData.NoAdsLimitedTimeData.GetRemainingSeconds();

                case ItemId.AutoXLimitedTime:
                    return (int)SecuredData.AutoXLimitedTimeData.GetRemainingSeconds();

                default:
                    return SecuredData.GetNonconsumableItemAmount(itemId);
            }
        }

        private void AddNonconsumableItem(string itemId, int amount, string source)
        {
            if (amount == 0)
                return;

            int finalBalance;
            switch (itemId)
            {
                case ItemId.NoAds_24h:
                    {
                        SecuredData.NoAdsLimitedTimeData.AddLimitedTime(24 * 60 * 60 * amount);
                        finalBalance = (int)SecuredData.NoAdsLimitedTimeData.GetRemainingSeconds();
                    }
                    break;

                case ItemId.NoAds_7Days:
                    {
                        SecuredData.NoAdsLimitedTimeData.AddLimitedTime(7 * 24 * 60 * 60 * amount);
                        finalBalance = (int)SecuredData.NoAdsLimitedTimeData.GetRemainingSeconds();
                    }
                    break;

                case ItemId.NoAdsLimitedTime:
                    {
                        SecuredData.NoAdsLimitedTimeData.AddLimitedTime(amount);
                        finalBalance = (int)SecuredData.NoAdsLimitedTimeData.GetRemainingSeconds();
                    }
                    break;

                case ItemId.AutoXLimitedTime:
                    {
                        SecuredData.AutoXLimitedTimeData.AddLimitedTime(amount);
                        finalBalance = (int)SecuredData.AutoXLimitedTimeData.GetRemainingSeconds();
                    }
                    break;

                default:
                    {
                        var item = SecuredData.AddNonconsumableItem(itemId, amount);
                        finalBalance = item?.Amount ?? 0;
                    }
                    break;
            }

            OnNonconsumableItemChanged?.Invoke(itemId, finalBalance);
            UserDataExtensions.TrackNonConsumableItemAdded(itemId, amount, finalBalance, source);
        }
        #endregion

        #region Collection Items Functions
        private void AddCollectionItem(CollectionItem item, string source)
        {
            if (item == null)
                return;

            if (CollectionData.AddCollectionItem(item))
            {
                TrackingData.AddNewCollectionItem(item);
                UserDataExtensions.TrackCollectionItemAdded(item, source);
            }
        }
        #endregion

        #region Restorable Resource Items
        public void UpdateRestorableResourceItem(string resourceId)
        {
            if (SecuredData.UpdateRestorableResourceItem(resourceId))
            {
                OnResourceItemChanged?.Invoke(resourceId, SecuredData.GetResourceItemAmount(resourceId));
            }
        }
        #endregion

        #region Purchase ShopItem
        public bool CanPurchaseShopItem(ShopItem shopItem)
        {
            if (shopItem == null)
                return false;

            var resourceAmount = GetResourceItemAmount(shopItem.Price.Id);
            return resourceAmount >= shopItem.Price.Value;
        }

        public bool PurchaseShopItem(ShopItem shopItem)
        {
            if (shopItem == null)
                return false;

            var result = SpendResourceItem(shopItem.Price.Id, shopItem.Price.Value, shopItem.GetAnalyticsBuyActionId());
            if (result)
            {
                ProcessPurchasedShopItem(shopItem);
            }

            return result;
        }

        public void ProcessPurchasedShopItem(ShopItem shopItem)
        {
            AddItems(shopItem.ItemsReward, shopItem.Id);
            TrackingData.AddShopItemBoughtTimes(shopItem);
        }
        #endregion

        #region No Ads
        public bool NoAdsActivated()
        {
            return SecuredData.NoAdsActivated();
        }
        #endregion

        #region AutoX

        public bool AutoXActivated()
        {
            return SecuredData.AutoXActivated();
        }
        #endregion

        #region Process Rewards
        public List<Item> AggregateItems(List<Item> source)
        {
            if (source == null || source.Count == 0) return null;
            var result = new List<Item>();

            foreach (var item in source)
            {
                var existing = result.Find(i => i.Id == item.Id);
                if (existing != null)
                {
                    existing.Amount += item.Amount;
                }
                else
                {
                    result.Add(new Item { Id = item.Id, Amount = item.Amount });
                }
            }

            return result;
        }

        public void ProcessRewardItem(Item item, List<Item> grantList, List<Item> uiList)
        {
            if (ItemId.IsRandomItem(item.Id))
            {
                var randomDef = Design.DesignDataHolder.Instance.RandomItemData.Get(item.Id);
                if (randomDef != null)
                {
                    for (var i = 0; i < Mathf.Max(1, item.Amount); i++)
                    {
                        var randomItems = randomDef.GetRandomItems();
                        foreach (var resolved in randomItems)
                        {
                            ProcessRewardItem(resolved, grantList, uiList);
                        }
                    }
                }

                return;
            }

            if (item.Id == ItemId.AutoXLimitedTime)
            {
                if (SecuredData.GetNonconsumableItemAmount(ItemId.AutoX) > 0)
                {
                    var hours = item.Amount / 3600;
                    grantList.Add(new Item { Id = ItemId.Coin, Amount = hours * 150 });
                    uiList.Add(new Item { Id = ItemId.Coin, Amount = hours * 150 });
                }
                else
                {
                    SecuredData.AutoXLimitedTimeData.AddLimitedTime(item.Amount);
                    uiList.Add(new Item { Id = ItemId.AutoXLimitedTime, Amount = item.Amount });
                }

                return;
            }

            if (item.Id == ItemId.NoAdsLimitedTime)
            {
                if (SecuredData.GetNonconsumableItemAmount(ItemId.NoAds) > 0)
                {
                    var hours = item.Amount / 3600;
                    grantList.Add(new Item { Id = ItemId.Coin, Amount = hours * 300 });
                    uiList.Add(new Item { Id = ItemId.Coin, Amount = hours * 300 });
                }
                else
                {
                    SecuredData.NoAdsLimitedTimeData.AddLimitedTime(item.Amount);
                    uiList.Add(new Item { Id = ItemId.NoAdsLimitedTime, Amount = item.Amount });
                }

                return;
            }

            grantList.Add(new Item { Id = item.Id, Amount = item.Amount });
            uiList.Add(new Item { Id = item.Id, Amount = item.Amount });
        }
        #endregion
    }
}