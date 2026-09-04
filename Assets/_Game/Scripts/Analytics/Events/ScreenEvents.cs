using System.Collections.Generic;

namespace Analytics.Events
{
    public class OpenScreenEvent : AnalyticsEvent
    {
        public override string EventName => EventNames.OpenScreen;

        public string ScreenId { get; set; }
        public int Level { get; set; }
        public int Coin { get; set; }
        public int BoosterQueen { get; set; }
        public int BoosterHint { get; set; }
        public int BoosterRandomMark { get; set; }

        public override Dictionary<string, object> ToParameters()
        {
            return new Dictionary<string, object>
            {
                { "screen_id", ScreenId },
                { "level", Level },
                { "coin", Coin },
                { "booster_queen", BoosterQueen },
                { "booster_hint", BoosterHint },
                { "booster_random_mark", BoosterRandomMark }
            };
        }
    }
}
