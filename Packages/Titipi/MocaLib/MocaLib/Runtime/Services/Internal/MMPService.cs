using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace Titipi.MocaLib.Runtime.Services.Internal
{
    public interface MMPService
    {
        void Initialize(MMPConfig mmpConfig);
        void LogEvent(string eventName);
        void LogEvent(string eventName, Dictionary<string, string> data);

        void LogAdRevenueToAdjust(AdImpressionData adImpressionData);
        void LogAdRevenueToAppsFlyer(AdImpressionData adImpressionData);

        void LogPurchaseToAdjust(Product product, string adjustToken);
        void LogPurchaseToAppsFlyer(Product product, string googlePublicKey);
    }
}
