#if MOCALIB_USE_BYTEBREW

using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing;

using ByteBrewSDK;

using Titipi.MocaLib.Runtime.Common;
using Titipi.MocaLib.Runtime.Services.Internal;

namespace Titipi.MocaLib.Runtime.Services
{
    public class ByteBrewAnalyticsService : IAnalyticsService
    {
        private const string TAG = "ByteBrewAnalyticsService";

        public void Initialize()
        {
            Utils.MocaLibLog(TAG, "Initialize");
            ByteBrew.InitializeByteBrew();
        }

        public void LogEvent(string eventName)
        {
            ByteBrew.NewCustomEvent(eventName);
        }

        public void LogEvent(string eventName, string parameterName, string parameterValue)
        {
            ByteBrew.NewCustomEvent(eventName, new Dictionary<string, string> { { parameterName, parameterValue } });
        }

        public void LogEvent(string eventName, Dictionary<string, object> data)
        {
            var dString = data.ToDictionary(k => k.Key, k => k.Value == null ? "null" : k.Value.ToString());

            ByteBrew.NewCustomEvent(eventName, dString);
        }

        public void LogUserProperty(string key, string value)
        {
            ByteBrew.SetCustomUserDataAttribute(key, value);
        }

        public void LogPurchase(string playMode, int level, Product product, string category)
        {
#if UNITY_ANDROID
            ByteBrew.TrackGoogleInAppPurchaseEvent("Google Play Store",
                product.metadata.isoCurrencyCode,
                (float) product.metadata.localizedPrice,
                product.definition.id,
                category,
                product.receipt,
                Utils.GetAndroidReceiptSignature(product)
            );
#elif UNITY_IOS
            ByteBrew.TrackiOSInAppPurchaseEvent("Apple App Store",
                product.metadata.isoCurrencyCode,
                (float) product.metadata.localizedPrice,
                product.definition.id,
                category,
                product.receipt
            );
#endif
        }

        public void LogAdRevenue(string playMode, int level, string location, AdImpressionData adImpressionData, Dictionary<string, string> customParamsDict = null)
        {
            // TODO
        }

        public void LogFirstOpen()
        {
            ByteBrew.NewCustomEvent("moca_first_open");
        }

        public void LogAppOpen()
        {
            ByteBrew.NewCustomEvent("moca_app_open");
        }

        public void LogSourceEvent(string currency, int amount, string itemType, string itemId)
        {
        }

        public void LogSinkEvent(string currency, int amount, string itemType, string itemId)
        {
        }
    }
}

#endif // MOCALIB_USE_BYTEBREW
