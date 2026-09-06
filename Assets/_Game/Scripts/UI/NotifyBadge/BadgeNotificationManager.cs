using System;
using System.Collections.Generic;
using Design;
using Design.Ids;
using Design.Utils;
using UnityEngine;
using UserDataPack;

namespace _Game.UI.NotifyBadge
{
    /// <summary>
    /// Centralized manager that tracks notification badge state.
    /// Call <see cref="SetNotification"/> / <see cref="ClearNotification"/> from game systems;
    /// <see cref="UINotifyBadge"/> components subscribe automatically.
    /// </summary>
    public class BadgeNotificationManager : SingletonComponent<BadgeNotificationManager>
    {
        /// <summary>Fired whenever a notification key's count changes. Parameter is the key.</summary>
        public event Action<string> OnNotificationChanged;

        private readonly Dictionary<string, int> _notifications = new();

        /// <summary>Set (or update) the notification count for a key. Count &lt;= 0 clears it.</summary>
        public void SetNotification(string key, int count = 1)
        {
            if (count <= 0)
            {
                ClearNotification(key);
                return;
            }

            bool changed = !_notifications.TryGetValue(key, out var prev) || prev != count;
            _notifications[key] = count;

            if (changed)
                OnNotificationChanged?.Invoke(key);
        }

        public void IncreaseNotification(string key, int amount = 1)
        {
            if (_notifications.TryGetValue(key, out var prev))
            {
                SetNotification(key, prev + amount);
            }
        }

        public void DecreaseNotification(string key, int amount = 1)
        {
            IncreaseNotification(key, -amount);
        }

        /// <summary>Remove the notification for a key.</summary>
        public void ClearNotification(string key)
        {
            if (_notifications.Remove(key))
                OnNotificationChanged?.Invoke(key);
        }

        /// <summary>Returns true when the key has a count > 0.</summary>
        public bool HasNotification(string key)
        {
            return _notifications.TryGetValue(key, out var c) && c > 0;
        }

        /// <summary>Returns true if any of the keys has a count > 0.</summary>
        public bool HasAnyNotification(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                if (HasNotification(key))
                    return true;
            }

            return false;
        }

        /// <summary>Returns the current count (0 if not set).</summary>
        public int GetCount(string key)
        {
            return _notifications.TryGetValue(key, out var c) ? c : 0;
        }

        /// <summary>Returns the total count across multiple keys.</summary>
        public int GetCount(IEnumerable<string> keys)
        {
            int total = 0;
            foreach (var key in keys)
            {
                total += GetCount(key);
            }

            return total;
        }

        /// <summary>Clear every notification at once.</summary>
        public void ClearAll()
        {
            var keys = new List<string>(_notifications.Keys);
            _notifications.Clear();
            foreach (var key in keys)
                OnNotificationChanged?.Invoke(key);
        }

        private void Start()
        {
            CheckShopBadges();
            RefreshUserProfileBadges();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                CheckShopBadges();
                RefreshUserProfileBadges();
            }
        }

        /// <summary>
        /// Checks all shop badge conditions: free reward + daily-limit items reset.
        /// Daily-limit badge only shows once per day (until user opens the shop).
        /// </summary>
        public void RefreshShopBadges()
        {
            CheckShopBadges();
        }

        private void CheckShopBadges()
        {
            var designDataHolder = DesignDataHolder.Instance;
            if (designDataHolder == null) return;

            var trackingData = UserData.Instance.TrackingData;
            if (designDataHolder.HasFreeCoinRewardInShop(out var freeCoinShopItem) &&
                (freeCoinShopItem.Price.Id == PriceId.Free ||
                !trackingData.IsDailyBadgeDismissedToday(BadgeNotificationType.Shop_FreeReward)))
                SetNotification(BadgeNotificationType.Shop_FreeReward);
            else
                ClearNotification(BadgeNotificationType.Shop_FreeReward);

            // Count how many daily-limit shop items are available (reset for a new day)
            var availableCount = 0;
            foreach (var shopItem in designDataHolder.NormalShopItemData.GetAllShopItems())
            {
                if (shopItem.Id == ShopItemId.FreeCoin || shopItem.Id == ShopItemId.AdsToCoin)
                    continue; // Free reward items are handled by a separate badge, so ignore them here

                if (shopItem.HasDailyPurchaseLimit && ShopItemUtils.IsShopItemAvailable(shopItem) && !trackingData.IsDailyBadgeDismissedToday(shopItem.Id))
                    availableCount++;
            }

            // New promotion offers
            availableCount += trackingData.NewPromotionOfferIds.Count;

            if (availableCount > 0)
                SetNotification(BadgeNotificationType.Shop_NewItems, availableCount);
            else
                ClearNotification(BadgeNotificationType.Shop_NewItems);
        }

        public void RefreshUserProfileBadges()
        {
            var newCollectionItems = UserData.Instance.TrackingData.NewIntCollectionItems;
            foreach (var data in newCollectionItems)
            {
                switch (data.Key)
                {
                    case ItemId.Avatar:
                        if (data.Values.Count > 0)
                            SetNotification(BadgeNotificationType.UserProfile_NewAvatar, data.Values.Count);
                        else
                            ClearNotification(BadgeNotificationType.UserProfile_NewAvatar);
                        break;

                    case ItemId.AvatarFrame:
                        if (data.Values.Count > 0)
                            SetNotification(BadgeNotificationType.UserProfile_NewAvatarFrame, data.Values.Count);
                        else
                            ClearNotification(BadgeNotificationType.UserProfile_NewAvatarFrame);
                        break;

                    case ItemId.ProfileBanner:
                        if (data.Values.Count > 0)
                            SetNotification(BadgeNotificationType.UserProfile_NewProfileBanner, data.Values.Count);
                        else
                            ClearNotification(BadgeNotificationType.UserProfile_NewProfileBanner);
                        break;
                }
            }

            if (!UserData.Instance.TrackingData.UserProfileChangeUserNameBadgeDismissed)
                SetNotification(BadgeNotificationType.UserProfile_ChangeUserName);
            else
                ClearNotification(BadgeNotificationType.UserProfile_ChangeUserName);
        }
    }
}
