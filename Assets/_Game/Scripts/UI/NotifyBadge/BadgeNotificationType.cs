
using Design.Ids;
using Design.Structures;

namespace _Game.UI.NotifyBadge
{
    /// <summary>
    /// String constants for notification badge keys.
    /// Add new keys here as needed — no recompilation of consumers required.
    /// </summary>
    public static class BadgeNotificationType
    {
        public const string NewFeatureAvailable = "new_feature_available";
        public const string NewFeature_ArtPuzzle = "new_feature_art_puzzle";
        public const string NewFeature_DayStreak = "new_feature_day_streak";
        public const string NewFeature_Customize = "new_feature_customize";
        public const string Shop_NewItems = "shop_new_items";
        public const string Shop_FreeReward = "shop_free_reward";
        public const string DailyChallenge_UnsolvedToday = "daily_challenge_unsolved_today";
        public const string DailyChallenge_MilestoneClaimable = "daily_challenge_milestone_claimable";
        public const string FortuneWheel_FreeSpinAvailable = "fortune_wheel_free_spin_available";
        public const string FortuneWheel_SpinAvailable = "fortune_wheel_spin_available";
        public const string UserProfile_NewAvatar = "user_profile_new_avatar";
        public const string UserProfile_NewAvatarFrame = "user_profile_new_avatar_frame";
        public const string UserProfile_NewProfileBanner = "user_profile_new_profile_banner";
        public const string UserProfile_ChangeUserName = "user_profile_change_user_name";

        /// <summary>Returns all defined keys (useful for Odin ValueDropdown).</summary>
        public static string[] All => new[]
        {
            NewFeatureAvailable,
            NewFeature_ArtPuzzle,
            NewFeature_DayStreak,
            NewFeature_Customize,
            Shop_NewItems,
            Shop_FreeReward,
            DailyChallenge_UnsolvedToday,
            DailyChallenge_MilestoneClaimable,
            FortuneWheel_FreeSpinAvailable,
            FortuneWheel_SpinAvailable,
            UserProfile_NewAvatar,
            UserProfile_NewAvatarFrame,
            UserProfile_NewProfileBanner,
            UserProfile_ChangeUserName,
        };

        public static string GetNewCollectionItemNotifyType(CollectionItem item)
        {
            switch (item.CollectionType)
            {
                case ItemId.Avatar:
                    return UserProfile_NewAvatar;

                case ItemId.AvatarFrame:
                    return UserProfile_NewAvatarFrame;

                case ItemId.ProfileBanner:
                    return UserProfile_NewProfileBanner;
            }

            return "";
        }
    }
}
