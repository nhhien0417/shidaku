using System.Globalization;
using Design;
using Design.Ids;
using MEC;
using Titipi.MocaLib.Runtime.Services;
using UserDataPack;

namespace _Game.Scripts.LiveOps
{
    public static class UserProperty
    {
        // Firebase Analytics user properties have specific limits:
        // Number of User Properties:
        //      A Firebase project can define up to 25 different custom user properties. Firebase Analytics automatically logs some user properties, but if additional data is required, up to 25 custom properties can be set.
        // User Property Name Length:
        //      The name of a custom user property can be up to 24 characters long. User property names must start with an alphabetic character and can only contain alphanumeric characters and underscores ("_").
        // User Property Value Length:
        //      The value associated with a custom user property can be up to 36 characters long. If a value exceeds this limit, it may be discarded.
        private const string USER_PROPERTY_CURRENT_LEVEL = "current_level";
        private const string USER_PROPERTY_CURRENT_COINS = "current_coins";
        private const string USER_PROPERTY_NOADS_ACTIVATED = "noads_activated";
        private const string USER_PROPERTY_AUTOX_ACTIVATED = "autox_activated";
        private const string USER_PROPERTY_APPROXIMATE_DEVICE_ID = "approximate_device_id";
        private const string USER_PROPERTY_TOTAL_PLAYTIME = "total_playtime";
        private const string USER_PROPERTY_PLAYTIME_GROUP = "playtime_group";
        private const string USER_PROPERTY_LEVEL_GROUP = "level_group";

        private static long _lastLoggedLevel = -1;
        private static int _lastLoggedLevelGroup = -1;
        public static void SetCurrentLevel(int level)
        {
            if (level != _lastLoggedLevel)
            {
                _lastLoggedLevel = level;
                MocaLib.Instance.AnalyticsManager.LogUserProperty(USER_PROPERTY_CURRENT_LEVEL, level.ToString());
            }

            var levelGroup = DesignDataHolder.Instance?.LevelGroupConfig?.GetGroupId(level) ?? 0;
            if (levelGroup != _lastLoggedLevelGroup)
            {
                _lastLoggedLevelGroup = levelGroup;
                MocaLib.Instance.AnalyticsManager.LogUserProperty(USER_PROPERTY_LEVEL_GROUP, levelGroup.ToString(CultureInfo.InvariantCulture));
            }
        }

        public static void SetCurrentCoins(int coins)
        {
            MocaLib.Instance.AnalyticsManager.LogUserProperty(USER_PROPERTY_CURRENT_COINS, coins.ToString());
        }

        public static void SetNoAdsActivated(bool status)
        {
            MocaLib.Instance.AnalyticsManager.LogUserProperty(USER_PROPERTY_NOADS_ACTIVATED, status ? "true" : "false");
        }

        public static void SetAutoXActivated(bool status)
        {
            MocaLib.Instance.AnalyticsManager.LogUserProperty(USER_PROPERTY_AUTOX_ACTIVATED, status ? "true" : "false");
        }

        public static void SetApproximateDeviceId(string deviceId)
        {
            MocaLib.Instance.AnalyticsManager.LogUserProperty(USER_PROPERTY_APPROXIMATE_DEVICE_ID, deviceId);
        }

        private static long _lastLoggedPlaytimeMinutes = -1;
        private static int _lastLoggedPlaytimeGroup = -1;
        public static void SetTotalPlaytime(long totalMilliseconds)
        {
            var totalMinutes = totalMilliseconds / 60_000L;
            if (totalMinutes != _lastLoggedPlaytimeMinutes)
            {
                _lastLoggedPlaytimeMinutes = totalMinutes;
                MocaLib.Instance.AnalyticsManager.LogUserProperty(USER_PROPERTY_TOTAL_PLAYTIME, totalMinutes.ToString(CultureInfo.InvariantCulture));
            }

            var playtimeGroup = DesignDataHolder.Instance?.PlaytimeGroupConfig?.GetGroupId(totalMinutes) ?? 0;
            if (playtimeGroup != _lastLoggedPlaytimeGroup)
            {
                _lastLoggedPlaytimeGroup = playtimeGroup;
                MocaLib.Instance.AnalyticsManager.LogUserProperty(USER_PROPERTY_PLAYTIME_GROUP, playtimeGroup.ToString(CultureInfo.InvariantCulture));
            }
        }

        public static void Initialize()
        {
            var userData = UserData.Instance;
            userData.OnResourceItemChanged += OnItemValueChanged;
            userData.OnNonconsumableItemChanged += OnItemValueChanged;

            SetCurrentLevel(userData.GameplayData.CurrentGameplayLevel);
            SetCurrentCoins(userData.SecuredData.GetResourceItemAmount(ItemId.Coin));
            RunNoAdsLimitedTimeCheck();
            RunAutoXLimitedTimeCheck();
            TotalPlaytimeTracker.EnableUserPropertySync();
        }

        #region Internal
        private static void OnItemValueChanged(string itemId, int value)
        {
            switch (itemId)
            {
                case ItemId.Coin:
                    SetCurrentCoins(value);
                    break;

                case ItemId.NoAds_7Days:
                case ItemId.NoAds_24h:
                case ItemId.NoAdsLimitedTime:
                    RunNoAdsLimitedTimeCheck();
                    break;
                case ItemId.NoAds:
                    SetNoAdsActivated(UserData.Instance.NoAdsActivated());
                    break;

                case ItemId.AutoXLimitedTime:
                    RunAutoXLimitedTimeCheck();
                    break;
                case ItemId.AutoX:
                    SetAutoXActivated(UserData.Instance.AutoXActivated());
                    break;
            }
        }

        private static CoroutineHandle _noAdsLimitedTimeCheck;
        private static void RunNoAdsLimitedTimeCheck()
        {
            Timing.KillCoroutines(_noAdsLimitedTimeCheck);
            var userData = UserData.Instance;
            var remainingSeconds = userData.SecuredData.NoAdsLimitedTimeData.GetRemainingSeconds();
            if (remainingSeconds > 0)
            {
                SetNoAdsActivated(true);
                _noAdsLimitedTimeCheck = Timing.CallDelayed(remainingSeconds, RunNoAdsLimitedTimeCheck);
            }
            else
            {
                SetNoAdsActivated(UserData.Instance.NoAdsActivated());
            }
        }

        private static CoroutineHandle _autoXLimitedTimeCheck;
        private static void RunAutoXLimitedTimeCheck()
        {
            Timing.KillCoroutines(_autoXLimitedTimeCheck);
            var userData = UserData.Instance;
            var remainingSeconds = userData.SecuredData.AutoXLimitedTimeData.GetRemainingSeconds();
            if (remainingSeconds > 0)
            {
                SetAutoXActivated(true);
                _autoXLimitedTimeCheck = Timing.CallDelayed(remainingSeconds, RunAutoXLimitedTimeCheck);
            }
            else
            {
                SetAutoXActivated(UserData.Instance.AutoXActivated());
            }
        }
        #endregion
    }
}
