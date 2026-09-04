using Analytics;
using Analytics.Events;

public static partial class Track
{
    public static class CostCenter
    {
        public static void AdRevenueSdk(string adFormat, string value, string location, string adNetwork, string currency = "USD")
        {
            AnalyticsManager.Instance.Track(new AdRevenueSdkEvent
            {
                PlayMode = AnalyticsContext.CurrentPlayMode,
                Level = AnalyticsContext.CurrentLevel,
                AdFormat = adFormat,
                Value = value,
                Currency = currency,
                Location = location,
                AdNetwork = adNetwork
            });
        }

        public static void Iap(string productId, string price, string currency, string placement)
        {
            AnalyticsManager.Instance.Track(new IapSdkEvent
            {
                PlayMode = AnalyticsContext.CurrentPlayMode,
                Level = AnalyticsContext.CurrentLevel,
                ProductId = productId,
                Value = price,
                Currency = currency,
                Placement = placement
            });
        }

        public static void ResourceSource(string resourceId, int amountAdded, int balance, string source)
        {
            AnalyticsManager.Instance.Track(new ResourceSourceEvent
            {
                PlayMode = AnalyticsContext.CurrentPlayMode,
                Level = AnalyticsContext.CurrentLevel,
                ResourceId = resourceId.ToAnalyticsItemId(),
                AmountAdded = amountAdded,
                Balance = balance,
                Source = source
            });
        }

        public static void ResourceSink(string resourceId, int amountSpent, int balance, string actionName)
        {
            AnalyticsManager.Instance.Track(new ResourceSinkEvent
            {
                PlayMode = AnalyticsContext.CurrentPlayMode,
                Level = AnalyticsContext.CurrentLevel,
                ResourceId = resourceId.ToAnalyticsItemId(),
                AmountSpent = amountSpent,
                Balance = balance,
                ActionName = actionName
            });
        }
    }
}
