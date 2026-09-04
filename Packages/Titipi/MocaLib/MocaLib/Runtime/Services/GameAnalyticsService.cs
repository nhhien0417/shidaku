#if MOCALIB_USE_GAMEANALYTICS

using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing;

using GameAnalyticsSDK;

using Titipi.MocaLib.Runtime.Common;
using Titipi.MocaLib.Runtime.Services.Internal;

namespace Titipi.MocaLib.Runtime.Services
{
    public class GameAnalyticsService : IAnalyticsService
    {
        private const string TAG = "GameAnalyticsService";

        public void Initialize()
        {
            Utils.MocaLibLog(TAG, "Initialize");
            GameAnalytics.Initialize();
        }

        public void LogEvent(string eventName)
        {
            GameAnalytics.NewDesignEvent(eventName);
        }

        public void LogEvent(string eventName, string parameterName, string parameterValue)
        {
            LogEvent(eventName, new Dictionary<string, object>
            {
                { parameterName, parameterValue }
            });
        }

        public void LogEvent(string eventName, Dictionary<string, object> data)
        {
            GameAnalytics.NewDesignEvent(eventName, data);
        }

        public void LogUserProperty(string key, string value)
        {
        }

        public void LogPurchase(string playMode, int level, Product product, string category)
        {
            // TODO
        }

        public void LogAdRevenue(string playMode, int level, string location, AdImpressionData adImpressionData, Dictionary<string, string> customParamsDict = null)
        {
            // TODO
        }

        public void LogFirstOpen()
        {
            GameAnalytics.NewDesignEvent("moca_first_open");
        }

        public void LogAppOpen()
        {
            GameAnalytics.NewDesignEvent("moca_app_open");
        }

        public void LogSourceEvent(string currency, int amount, string itemType, string itemId)
        {
            GameAnalytics.NewResourceEvent(GAResourceFlowType.Source, currency, amount, itemType, itemId);
        }

        public void LogSinkEvent(string currency, int amount, string itemType, string itemId)
        {
            GameAnalytics.NewResourceEvent(GAResourceFlowType.Sink, currency, amount, itemType, itemId);
        }
    }
}

#endif // MOCALIB_USE_GAMEANALYTICS
