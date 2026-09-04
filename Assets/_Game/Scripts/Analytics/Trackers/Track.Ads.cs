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
    }
}
