using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Design.Ids
{
    public abstract class ItemId // IMPORTANT: Remember to update ConsumableItems, NonconsumableItems when add new id
    {
        // Consumable items
        public const string Coin = "coin";

        // Boosters
        public const string Booster_1 = "booster_1";
        public const string Booster_2 = "booster_2";
        public const string Booster_3 = "booster_3";

        // Non-consumable items
        public const string NoAds = "noads";
        public const string NoAds_24h = "noads_24h";
        public const string NoAds_7Days = "noads_7days";
        public const string NoAdsLimitedTime = "noads_limited_time"; // The amount of this item is the total seconds of NoAds
        public const string AutoX = "autox";
        public const string AutoXLimitedTime = "autox_limited_time"; // The amount of this item is the total seconds of AutoX activation remaining
        public const string UnlockAllArtPuzzles = "unlock_all_art_puzzles";

        // Gameplay items
        public const string Lives = "lives";

        // Random items
        public const string FortuneWheelGift1 = "fortune_wheel_gift_1";
        public const string FortuneWheelGift2 = "fortune_wheel_gift_2";
        public const string RandomBooster = "random_booster";

        // Collection items
        public const string Avatar = "avatar";
        public const string AvatarFrame = "avatar_frame";
        public const string ProfileBanner = "profile_banner";
        public const string CustomizeQueen = "customize_queen";
        public const string CustomizeX = "customize_x";

        public static readonly string[] All =
        {
            // Consumable items
            Coin,

            // Gameplay items
            // Like extra slots, extra time, extra moves, ...

            // Boosters
            Booster_1,
            Booster_2,
            Booster_3,

            // Non-consumable items
            NoAds,
            NoAds_24h,
            NoAds_7Days,
            NoAdsLimitedTime,
            AutoX,
            AutoXLimitedTime,
            UnlockAllArtPuzzles,

            // Gameplay items
            Lives,

            // Random items
            FortuneWheelGift1,
            FortuneWheelGift2,
            RandomBooster,

            // Collection items
            Avatar,
            AvatarFrame,
            ProfileBanner,

            // Customize items
            CustomizeQueen,
            CustomizeX,
        };

        #region Booster Items
        public static readonly HashSet<string> AllBoosters = new()
        {
            Booster_1,
            Booster_2,
            Booster_3,
        };

        public static bool IsBooster(string itemId)
        {
            return AllBoosters.Contains(itemId);
        }
        #endregion

        #region Consumable Items
        private static readonly HashSet<string> ConsumableItems = new()
        {
            Coin,

            Booster_1,
            Booster_2,
            Booster_3,
        };

        public static bool IsConsumable(string itemId)
        {
            return ConsumableItems.Contains(itemId);
        }
        #endregion

        #region Non-consumable Items
        private static readonly HashSet<string> NonconsumableItems = new()
        {
            NoAds,
            NoAds_24h,
            NoAds_7Days,
            NoAdsLimitedTime,
            AutoX,
            AutoXLimitedTime,
            UnlockAllArtPuzzles,
        };

        public static bool IsNonConsumable(string itemId)
        {
            return NonconsumableItems.Contains(itemId);
        }
        #endregion

        #region Timed Items
        private static readonly HashSet<string> TimedItems = new()
        {
            NoAds_24h,
            NoAds_7Days,
            NoAdsLimitedTime,
            AutoXLimitedTime,
        };

        public static bool IsTimedItem(string itemId)
        {
            return TimedItems.Contains(itemId);
        }
        #endregion

        #region Restorable Items
        public static bool IsRestorable(string itemId)
        {
            return DesignDataHolder.Instance?.RestorableItems?.Get(itemId) != null;
        }
        #endregion

        #region Gameplay Items
        public static readonly HashSet<string> GameplayItems = new()
        {
            Lives,
        };

        public static bool IsGameplayItem(string itemId)
        {
            return GameplayItems.Contains(itemId);
        }
        #endregion

        #region No Ads Items
        public static readonly HashSet<string> NoAdsItems = new()
        {
            NoAds,
            NoAds_24h,
            NoAds_7Days,
            NoAdsLimitedTime,
        };

        public static bool IsNoAdsItem(string itemId)
        {
            return NoAdsItems.Contains(itemId);
        }
        #endregion

        #region In-app Purchase Restorable
        public static readonly HashSet<string> InappPurchaseRestorableItems = new()
        {
            NoAds,
            UnlockAllArtPuzzles,
        };

        public static bool IsInappPurchaseRestorable(string itemId)
        {
            return InappPurchaseRestorableItems.Contains(itemId) && IsNonConsumable(itemId);
        }
        #endregion

        #region Random Items
        public static readonly HashSet<string> AllRandomItems = new()
        {
            RandomBooster,
            FortuneWheelGift1,
            FortuneWheelGift2,
        };

        public static bool IsRandomItem(string itemId)
        {
            return DesignDataHolder.Instance?.RandomItemData?.Get(itemId) != null;
        }
        #endregion

        #region Collection Items
        public static readonly HashSet<string> CollectionItems = new()
        {
            Avatar,
            AvatarFrame,
            ProfileBanner,

            CustomizeQueen,
            CustomizeX,
        };

        public static bool IsCollectionItem(string itemId)
        {
            return CollectionItems.Contains(itemId);
        }
        #endregion

        public static bool HasOnlyOneInstance(string itemId)
        {
            return itemId switch
            {
                NoAds => true,
                AutoX => true,
                _ => false,
            };
        }

        public static string GetPluralId(string itemId)
        {
            return itemId switch
            {
                Coin => "coins",
                _ => itemId
            };
        }

        public static string GetItemName(string itemId)
        {
            return itemId switch
            {
                Coin => "Coin",
                NoAds => "No Ads",
                NoAds_7Days => "No Ads (7 Days)",
                NoAds_24h => "No Ads 24h",
                NoAdsLimitedTime => "No Ads Limited Time",
                AutoXLimitedTime => "AutoX",
                _ => itemId
            };
        }

        public static string GetItemShortName(string itemId)
        {
            return itemId switch
            {
                _ => itemId
            };
        }
    }

    public static class ItemIdShortCutUtils
    {
        public static string ToPluralId(this string itemId)
        {
            return ItemId.GetPluralId(itemId);
        }

        public static string ToItemName(this string itemId)
        {
            return ItemId.GetItemName(itemId);
        }

        public static string ToItemShortName(this string itemId)
        {
            return ItemId.GetItemShortName(itemId);
        }
    }
}