using Analytics;
using Analytics.Events;

public static partial class Track
{
    public static class Ads
    {
        public static void InterRequest(string placement)
        {
            AnalyticsManager.Instance.Track(new AdInterRequestEvent
            {
                Level = AnalyticsContext.CurrentLevel,
                PlayMode = AnalyticsContext.CurrentPlayMode,
                Placement = placement
            });
        }

        public static void InterShow(string placement)
        {
            AnalyticsManager.Instance.Track(new AdInterShowEvent
            {
                Level = AnalyticsContext.CurrentLevel,
                PlayMode = AnalyticsContext.CurrentPlayMode,
                Placement = placement
            });
        }

        public static void RewardRequest(string placement)
        {
            AnalyticsManager.Instance.Track(new AdRewardRequestEvent
            {
                Level = AnalyticsContext.CurrentLevel,
                PlayMode = AnalyticsContext.CurrentPlayMode,
                Placement = placement
            });
        }

        public static void RewardComplete(string placement, string rewardName, string rewards, int value)
        {
            AnalyticsManager.Instance.Track(new AdRewardCompleteEvent
            {
                Level = AnalyticsContext.CurrentLevel,
                PlayMode = AnalyticsContext.CurrentPlayMode,
                Placement = placement,
                RewardName = rewardName,
                Rewards = rewards,
                Value = value
            });
        }

        private static int _interAdCount;
        private static int _rvAdCount;
        private static double _adRevenue;

        public static void OnInterAd(float durationSeconds = 0f)
        {
            _interAdCount++;
        }

        public static void OnRvAd(float durationSeconds = 0f)
        {
            _rvAdCount++;
        }

        public static void OnAdRevenue(double value)
        {
            _adRevenue += System.Math.Max(0d, value);
        }
    }
}
