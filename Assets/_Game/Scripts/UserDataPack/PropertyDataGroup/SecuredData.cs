using System;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using Design;
using Design.Ids;
using UnityEngine;
using UserDataPack.Structures;

namespace UserDataPack.PropertyDataGroup
{
    /// <summary>
    /// All fields in this class is public for serialization. Please do not access or modify them directly outside of this class. Use the methods instead (create if needed).
    /// </summary>
    [Serializable]
    public class SecuredData : IUserDataPropertyDataGroup
    {
        public void FixData()
        {
            if (ResourceItems == null)
            {
                ResourceItems = new();
            }

            if (NonConsumableItems == null)
            {
                NonConsumableItems = new();
            }

            if (LastResourceItemRestoreTime == null)
            {
                LastResourceItemRestoreTime = new();
            }

            if (NoAdsLimitedTimeData == null)
            {
                NoAdsLimitedTimeData = new();
            }

            if (AutoXLimitedTimeData == null)
            {
                AutoXLimitedTimeData = new();
            }
        }

        public void UpdateData()
        {
            foreach (var v in LastResourceItemRestoreTime)
            {
                UpdateRestorableResourceItem(v.Key);
            }
        }

        #region Common
        // Nothings here yet, but can be used for common secured data in the future
        #endregion

        #region Resource Item

        public List<Item> ResourceItems = new();

        public Item AddResourceItem(string resourceId, int amount)
        {
            var resource = ResourceItems.Find(item => item.Id == resourceId);
            if (resource == null)
            {
                resource = new Item
                {
                    Id = resourceId,
                    Amount = amount
                };
                ResourceItems.Add(resource);
            }
            else
            {
                resource.Amount += amount;
            }

            return resource;
        }

        private Item GetResourceItem(string resourceId)
        {
            var resource = ResourceItems.Find(item => item.Id == resourceId);
            if (resource != null)
            {
                return resource;
            }

            return null;
        }

        private Item GetOrCreateResourceItem(string resourceId)
        {
            var resource = GetResourceItem(resourceId);
            if (resource == null)
            {
                resource = new Item
                {
                    Id = resourceId,
                    Amount = 0
                };
                ResourceItems.Add(resource);
            }
            return resource;
        }

        public int GetResourceItemAmount(string resourceId)
        {
            var resource = GetResourceItem(resourceId);
            return resource?.Amount ?? 0;
        }

        public bool SpendResourceItem(string resourceId, int amount, out Item resourceItem)
        {
            resourceItem = GetResourceItem(resourceId);
            if (resourceItem != null && resourceItem.Amount >= amount)
            {
                UpdateLastResourceItemRestoreTimeOnUse(resourceItem);
                resourceItem.Amount -= amount;
                return true;
            }

            return false;
        }

        public bool ResourceItemDataExists(string resourceId)
        {
            return ResourceItems.Find(item => item.Id == resourceId) != null;
        }

        #endregion

        #region Non-consumable Items

        public List<Item> NonConsumableItems = new();

        public Item AddNonconsumableItem(string itemId, int amount)
        {
            var item = NonConsumableItems.Find(item => item.Id == itemId);
            if (item == null)
            {
                item = new Item
                {
                    Id = itemId,
                    Amount = amount
                };
                NonConsumableItems.Add(item);
            }
            else
            {
                item.Amount += amount;
            }

            return item;
        }

        public Item GetNonconsumableItem(string itemId)
        {
            var item = NonConsumableItems.Find(item => item.Id == itemId);
            if (item != null)
            {
                return item;
            }

            return null;
        }

        public int GetNonconsumableItemAmount(string itemId)
        {
            var item = GetNonconsumableItem(itemId);
            return item?.Amount ?? 0;
        }
        #endregion

        #region Restorable Resource Item

        public List<KeyValue<string>> LastResourceItemRestoreTime = new();

        public DateTime GetLastResourceItemRestoreTime(string resourceId)
        {
            var resource = LastResourceItemRestoreTime.Find(item => item.Key == resourceId);
            if (resource != null)
            {
                return resource.Value.ToDateTime();
            }

            return default;
        }

        public void SetLastResourceItemRestoreTime(string resourceId, DateTime time)
        {
            var resource = LastResourceItemRestoreTime.Find(item => item.Key == resourceId);
            if (resource != null)
            {
                resource.Value = time.ToTicksAsString();
            }
            else
            {
                LastResourceItemRestoreTime.Add(new ()
                {
                    Key = resourceId,
                    Value = time.ToTicksAsString()
                });
            }
        }

        public bool UpdateRestorableResourceItem(string resourceId)
        {
            if (!ItemId.IsRestorable(resourceId))
                return false;

            var lastRestoreTime = LastResourceItemRestoreTime.Find(item => item.Key == resourceId);
            if (lastRestoreTime == null)
            {
                if (!DateTimeManager.IsUpToDate)
                    return false;

                lastRestoreTime = new ()
                {
                    Key = resourceId,
                    Value = DateTimeManager.Now.ToTicksAsString()
                };
                LastResourceItemRestoreTime.Add(lastRestoreTime);
            }

            return UpdateRestorableResourceItem(lastRestoreTime);
        }

        private bool UpdateRestorableResourceItem(KeyValue<string> lastRestoreTime)
        {
            var restoreDesign = DesignDataHolder.Instance?.RestorableItems?.Get(lastRestoreTime.Key);
            if (restoreDesign == null)
            {
                Debug.LogError($"Restorable item design not found for {lastRestoreTime.Key}");
                return false;
            }
            var threshold = restoreDesign.StopRestoreThreshold;

            var item = GetOrCreateResourceItem(lastRestoreTime.Key);
            if (item.Amount >= threshold)
                return false;

            var now = DateTimeManager.Now;
            var timeToRestore = (now - lastRestoreTime.Value.ToDateTime()).TotalSeconds;

            if (timeToRestore >= restoreDesign.RestoreTimeInSeconds)
            {
                // Restore the item
                var restoreMultiplier = (long)timeToRestore / restoreDesign.RestoreTimeInSeconds;
                var restoreAmount = restoreDesign.RestoreAmount * (int)restoreMultiplier;
                item.Amount = Math.Clamp(item.Amount + restoreAmount, int.MinValue, threshold);

                // Update last restore time
                var remainingTime = timeToRestore - (restoreDesign.RestoreTimeInSeconds * restoreMultiplier);
                var newLastRestoreTime = now.AddSeconds(-remainingTime);
                SetLastResourceItemRestoreTime(lastRestoreTime.Key, newLastRestoreTime);

                return true;
            }

            return false;
        }

        private void UpdateLastResourceItemRestoreTimeOnUse(Item item)
        {
            var restoreDesign = DesignDataHolder.Instance?.RestorableItems?.Get(item.Id);
            if (restoreDesign == null)
                return;

            var threshold = restoreDesign.StopRestoreThreshold;
            if (item.Amount != threshold)
                return;

            if (!DateTimeManager.IsUpToDate)
                return;

            SetLastResourceItemRestoreTime(item.Id, DateTimeManager.Now);
        }

        #endregion

        #region No Ads
        public NoAdsLimitedTimeData NoAdsLimitedTimeData = new();

        public bool NoAdsActivated()
        {
            return GetNonconsumableItemAmount(ItemId.NoAds) > 0 || NoAdsLimitedTimeData.IsActivated();
        }
        #endregion

        #region Auto X
        public LimitedTimeData AutoXLimitedTimeData = new();

        public bool AutoXActivated()
        {
            return GetNonconsumableItemAmount(ItemId.AutoX) > 0 || AutoXLimitedTimeData.IsActivated();
        }
        #endregion
    }
}