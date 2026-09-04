using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace Titipi.MocaLib.Runtime.Services.Internal
{
    public interface IAnalyticsService
    {
        void Initialize();
        void LogEvent(string eventName);
        void LogEvent(string eventName, string parameterName, string parameterValue);
        void LogEvent(string eventName, Dictionary<string, object> data);
        void LogUserProperty(string key, string value);
        void LogPurchase(string playMode, int level, Product product, string category);
        void LogAdRevenue(string playMode, int level, string location, AdImpressionData adImpressionData, Dictionary<string, string> customParamsDict = null);
        void LogFirstOpen();
        void LogAppOpen();
        void LogSourceEvent(string currency, int amount, string itemType, string itemId);
        void LogSinkEvent(string currency, int amount, string itemType, string itemId);
    }
}
