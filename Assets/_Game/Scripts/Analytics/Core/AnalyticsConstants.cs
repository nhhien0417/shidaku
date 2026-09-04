namespace Analytics
{
    public static class EventNames
    {
        // Cost Center
        public const string AdRevenueSdk = "ad_revenue_sdk";
        public const string IapSdk = "iap_sdk";
        public const string ResourceSource = "resource_source";
        public const string ResourceSink = "resource_sink";

        // Tutorial
        public const string TutorialStep = "tutorial_step";

        // Gameplay
        public const string LevelStart = "level_start";
        public const string LevelEnd = "level_end";
        public const string LevelFail = "level_fail";
        public const string LevelContinue = "level_continue";

        // Checkpoint
        public const string CheckpointLevel = "checkpoint_level";

        // Booster
        public const string BoosterSpend = "booster_spend";
        public const string BoosterEarn = "booster_earn";

        // Screen
        public const string OpenScreen = "open_screen";

        // Ads
        public const string AdInterRequest = "ad_inter_request";
        public const string AdInterShow = "ad_inter_show";
        public const string AdRewardRequest = "ad_reward_request";
        public const string AdRewardComplete = "ad_reward_complete";
    }

    public static class Placement
    {
        public const string MainGameplay = "main_gameplay";
        public const string HomeScreen = "home_screen";
        public const string ShopScreen = "shop_screen";
        public const string UIDailyChallenge = "ui_daily_challenge";
        public const string UIFortuneWheel = "ui_fortune_wheel";
        public const string UIDayStreak = "ui_day_streak";
        public const string UICustomize = "ui_customize";
        public const string UIArtPuzzle = "ui_art_puzzle";
        public const string UIArtPuzzleUnlockAll = "ui_art_puzzle_unlock_all";
        public const string UIUserProfile = "ui_user_profile";
        public const string UILeaderboard = "ui_leaderboard";
        public const string UIGetMoreBooster = "ui_get_more_booster";
    }

    public static class PlayMode
    {
        public const string Normal = "normal";
        public const string ArtPuzzle = "art_puzzle";
        public const string DailyChallenge = "daily_challenge";
    }

    public static class BaseMode
    {
        public const string None = "";
        public const string Normal = "normal";
    }

    public static class TutorialStatus
    {
        public const string Start = "start";
        public const string Complete = "complete";
    }

    public static class LevelFailReason
    {
        public const string OutOfHeart = "out_of_heart";
        public const string Quit = "quit";
        public const string Skip = "skip";
    }

    public static class AdFormat
    {
        public const string Banner = "banner";
        public const string Rewarded = "rewarded";
        public const string Interstitial = "interstitial";
        public const string Audio = "audio";
    }

    public static class AdReason
    {
        public const string IvCompleteLevel = "iv_complete_level";
        public const string IvFailLevel = "iv_fail_level";
        public const string RvContinueHeart = "rv_continue_heart";
        public const string RvHint = "rv_hint";
        public const string RvQueen = "rv_queen";
        public const string RvRandomMark = "rv_random_mark";
        public const string RVAutoX = "rv_autoX";
        public const string RvCompleteReward = "rv_complete_reward";
        public const string RvCoinsReward = "rv_coins_reward";
        public const string RvUnlockDaily = "rv_unlock_daily";
        public const string RvWheelSpin = "rv_wheel_spin";
        public const string RvRestoreStreak = "rv_restore_streak";
        public const string RvCustomizeItem = "rv_customize_item";
    }

    public static class BoosterName
    {
        public const string Hint = "booster_hint";
        public const string Queen = "booster_queen";
        public const string RandomMark = "booster_random_mark";

        public static string FromItemId(string itemId)
        {
            return itemId switch
            {
                Design.Ids.ItemId.Booster_1 => Queen,
                Design.Ids.ItemId.Booster_2 => Hint,
                Design.Ids.ItemId.Booster_3 => RandomMark,
                _ => itemId
            };
        }
    }

    public static class AnalyticsItemId
    {
        public static string ToAnalyticsItemId(this string itemId)
        {
            switch (itemId)
            {
                case Design.Ids.ItemId.Booster_1:
                case Design.Ids.ItemId.Booster_2:
                case Design.Ids.ItemId.Booster_3:
                    return BoosterName.FromItemId(itemId);

                default:
                    return itemId;
            }
        }
    }
}
