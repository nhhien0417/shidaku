using System.Collections.Generic;

namespace Analytics.Events
{
    public class AdInterRequestEvent : AnalyticsEvent
    {
        public override string EventName => EventNames.AdInterRequest;

        public int Level { get; set; }
        public string PlayMode { get; set; }
        public string Placement { get; set; }

        public override Dictionary<string, object> ToParameters()
        {
            return new Dictionary<string, object>
            {
                { "level", Level },
                { "play_mode", PlayMode },
                { "placement", Placement }
            };
        }
    }

    public class AdInterShowEvent : AnalyticsEvent
    {
        public override string EventName => EventNames.AdInterShow;

        public int Level { get; set; }
        public string PlayMode { get; set; }
        public string Placement { get; set; }

        public override Dictionary<string, object> ToParameters()
        {
            return new Dictionary<string, object>
            {
                { "level", Level },
                { "play_mode", PlayMode },
                { "placement", Placement }
            };
        }
    }

    public class AdRewardRequestEvent : AnalyticsEvent
    {
        public override string EventName => EventNames.AdRewardRequest;

        public int Level { get; set; }
        public string PlayMode { get; set; }
        public string Placement { get; set; }

        public override Dictionary<string, object> ToParameters()
        {
            return new Dictionary<string, object>
            {
                { "level", Level },
                { "play_mode", PlayMode },
                { "placement", Placement }
            };
        }
    }

    public class AdRewardCompleteEvent : AnalyticsEvent
    {
        public override string EventName => EventNames.AdRewardComplete;

        public int Level { get; set; }
        public string PlayMode { get; set; }
        public string Placement { get; set; }
        public string RewardName { get; set; }
        public string Rewards { get; set; }
        public int Value { get; set; }

        public override Dictionary<string, object> ToParameters()
        {
            return new Dictionary<string, object>
            {
                { "level", Level },
                { "play_mode", PlayMode },
                { "placement", Placement },
                { "reward_name", RewardName },
                { "rewards", Rewards },
                { "value", Value }
            };
        }
    }
}
