using System.Collections.Generic;

namespace Analytics.Events
{
    public class BoosterSpendEvent : AnalyticsEvent
    {
        public override string EventName => EventNames.BoosterSpend;

        public int Level { get; set; }
        public string PlayMode { get; set; }
        public string PuzzleId { get; set; }
        public string BoosterName { get; set; }

        public override Dictionary<string, object> ToParameters()
        {
            return new Dictionary<string, object>
            {
                { "level", Level },
                { "play_mode", PlayMode },
                { "puzzle_id", PuzzleId },
                { "booster_name", BoosterName }
            };
        }
    }

    public class BoosterEarnEvent : AnalyticsEvent
    {
        public override string EventName => EventNames.BoosterEarn;

        public string BoosterName { get; set; }
        public int Value { get; set; }
        public string Source { get; set; }

        public override Dictionary<string, object> ToParameters()
        {
            return new Dictionary<string, object>
            {
                { "booster_name", BoosterName },
                { "value", Value },
                { "source", Source }
            };
        }
    }
}
