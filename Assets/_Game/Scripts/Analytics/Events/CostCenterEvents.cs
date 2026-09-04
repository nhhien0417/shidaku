using System.Collections.Generic;

namespace Analytics.Events
{
    public class AdRevenueSdkEvent : AnalyticsEvent
    {
        public override string EventName => EventNames.AdRevenueSdk;

        public string PlayMode { get; set; }
        public int Level { get; set; }
        public string AdFormat { get; set; }
        public string Value { get; set; }
        public string Currency { get; set; } = "USD";
        public string Location { get; set; }
        public string AdNetwork { get; set; }

        public override Dictionary<string, object> ToParameters()
        {
            return new Dictionary<string, object>
            {
                { "play_mode", PlayMode },
                { "level", Level },
                { "ad_format", AdFormat },
                { "value", Value },
                { "currency", Currency },
                { "location", Location },
                { "ad_network", AdNetwork }
            };
        }
    }

    public class IapSdkEvent : AnalyticsEvent
    {
        public override string EventName => EventNames.IapSdk;

        public string PlayMode { get; set; }
        public int Level { get; set; }
        public string ProductId { get; set; }
        public string Value { get; set; }
        public string Currency { get; set; }
        public string Placement { get; set; }

        public override Dictionary<string, object> ToParameters()
        {
            return new Dictionary<string, object>
            {
                { "play_mode", PlayMode },
                { "level", Level },
                { "product_id", ProductId },
                { "value", Value },
                { "currency", Currency },
                { "placement", Placement }
            };
        }
    }
    
    public class ResourceSourceEvent : AnalyticsEvent
    {
        public override string EventName => EventNames.ResourceSource;

        public string PlayMode { get; set; }
        public int Level { get; set; }
        public string ResourceId { get; set; }
        public int AmountAdded { get; set; }
        public int Balance { get; set; }
        public string Source { get; set; }

        public override Dictionary<string, object> ToParameters()
        {
            return new Dictionary<string, object>
            {
                { "play_mode", PlayMode },
                { "level", Level },
                { "name", ResourceId },
                { "amount", AmountAdded },
                { "balance", Balance },
                { "item", Source }
            };
        }
    }
    
    public class ResourceSinkEvent : AnalyticsEvent
    {
        public override string EventName => EventNames.ResourceSink;

        public string PlayMode { get; set; }
        public int Level { get; set; }
        public string ResourceId { get; set; }
        public int AmountSpent { get; set; }
        public int Balance { get; set; }
        public string ActionName { get; set; }

        public override Dictionary<string, object> ToParameters()
        {
            return new Dictionary<string, object>
            {
                { "play_mode", PlayMode },
                { "level", Level },
                { "name", ResourceId },
                { "amount", AmountSpent },
                { "balance", Balance },
                { "item", ActionName }
            };
        }
    }
}
