using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Purchasing;

#if !UNITY_WEBGL
using Firebase.Analytics;
using Firebase.Crashlytics;
#endif

using Titipi.MocaLib.Runtime.Common;
using Titipi.MocaLib.Runtime.Services.Internal;

namespace Titipi.MocaLib.Runtime.Services
{
    public class FirebaseAnalyticsService : IAnalyticsService
    {
        private const string TAG = "FirebaseAnalyticsService";

#if MOCALIB_USE_COST_CENTER
        private const string LIFETIME_REVENUE_KEY = "moca_lifetime_ad_revenue";
#endif

        public void Initialize()
        {
#if !UNITY_WEBGL
            Utils.MocaLibLog(TAG, "Initialize");
#endif
        }

        public void LogEvent(string eventName)
        {
#if !UNITY_WEBGL
            FirebaseAnalytics.LogEvent(eventName);
#endif
        }

        public void LogEvent(string eventName, string parameterName, string parameterValue)
        {
            FirebaseAnalytics.LogEvent(eventName, parameterName, parameterValue);
        }

        public void LogEvent(string eventName, Dictionary<string, object> data)
        {
#if !UNITY_WEBGL
            FirebaseAnalytics.LogEvent(eventName, DictToFirebaseParameters(data));
#endif
        }

        public void LogUserProperty(string key, string value)
        {
#if !UNITY_WEBGL
            FirebaseAnalytics.SetUserProperty(key, value);
#endif
        }

        public void LogPurchase(string playMode, int level, Product product, string category)
        {
#if UNITY_WEBGL
            return;
#endif

            // Firebase auto tracks IAP.

#if MOCALIB_USE_COST_CENTER
            var iapParameters = new[]
            {
                new Parameter("play_mode", playMode),
                new Parameter("level", level),
                new Parameter("value", (double) product.metadata.localizedPrice),
                new Parameter("currency", product.metadata.isoCurrencyCode),
                new Parameter("product_id", product.definition.id),
            };

            FirebaseAnalytics.LogEvent("iap_sdk", iapParameters);
#endif
        }

        public void LogAdRevenue(string playMode, int level, string location, AdImpressionData adImpressionData, Dictionary<string, string> customParamsDict = null)
        {
#if UNITY_WEBGL
            return;
#endif

            string adPlatform;

#if MOCALIB_AD_PROVIDER_APPLOVIN
            adPlatform = "AppLovin";
#elif MOCALIB_AD_PROVIDER_LEVELPLAY
            adPlatform = "ironSource";
#else
            adPlatform = "Unknown";
#endif

            var adImpressionEvent = new List<Parameter>
            {
                new Parameter("ad_platform", adPlatform),
                new Parameter("ad_source", adImpressionData.AdSource),
                new Parameter("ad_unit_name", adImpressionData.AdUnitId),
                new Parameter("ad_format", adImpressionData.AdFormat),
                new Parameter("value", adImpressionData.Value),
                new Parameter("currency", "USD")
            };

            var impressionParametersArray = adImpressionEvent.ToArray();
            FirebaseAnalytics.LogEvent("ad_impression", impressionParametersArray);

#if MOCALIB_USE_COST_CENTER
            var valueMicros = (long) Math.Round(Math.Max(0, adImpressionData.Value) * 1_000_000);
            var lifetimeRevenue = UpdateLifetimeRevenue(adImpressionData.Value);

            var adRevenueSdkEvent = new List<Parameter>
            {
                new Parameter("ad_platform", adPlatform),
                new Parameter("ad_source", adImpressionData.AdSource),
                new Parameter("ad_unit_name", adImpressionData.AdUnitId),
                new Parameter("ad_format", adImpressionData.AdFormat),
                new Parameter("value", adImpressionData.Value),
                new Parameter("currency", "USD"),

                new Parameter("play_mode", playMode),
                new Parameter("level", level),
                new Parameter("location", location),
                new Parameter("ad_network", adPlatform),
                new Parameter("creative_id", adImpressionData.CreativeId ?? ""),
                new Parameter("instance_id", adImpressionData.InstanceId ?? ""),
                new Parameter("precision", adImpressionData.Precision ?? ""),
                new Parameter("value_micros", valueMicros),
                new Parameter("lifetime_revenue", lifetimeRevenue),
            };

            if (customParamsDict != null && customParamsDict.Count > 0)
            {
                foreach (var customParam in customParamsDict)
                {
                    adRevenueSdkEvent.Add(new Parameter(customParam.Key, customParam.Value));
                }
            }

            impressionParametersArray = adRevenueSdkEvent.ToArray();

            if (string.Equals(adImpressionData.AdFormat, "APPOPEN"))
            {
                FirebaseAnalytics.LogEvent("ad_revenue_addition", impressionParametersArray);
            }
            else
            {
                FirebaseAnalytics.LogEvent("ad_revenue_sdk", impressionParametersArray);
            }
#endif
        }

        public void LogFirstOpen()
        {
            // No implementation. Firebase auto tracks First Open.
        }

        public void LogAppOpen()
        {
            // No implementation. Firebase auto tracks App Open.
        }

        public void LogSourceEvent(string currency, int amount, string itemType, string itemId)
        {
        }

        public void LogSinkEvent(string currency, int amount, string itemType, string itemId)
        {
        }

#if MOCALIB_USE_COST_CENTER
        private static double UpdateLifetimeRevenue(double value)
        {
            if (value <= 0 || double.IsNaN(value) || double.IsInfinity(value)) value = 0;

            var stored = PlayerPrefs.GetString(LIFETIME_REVENUE_KEY, "0");
            if (!double.TryParse(stored, NumberStyles.Float, CultureInfo.InvariantCulture, out var current))
            {
                current = 0;
            }

            var updated = current + value;
            PlayerPrefs.SetString(LIFETIME_REVENUE_KEY, updated.ToString("R", CultureInfo.InvariantCulture));
            PlayerPrefs.Save();

            return updated;
        }
#endif

#if !UNITY_WEBGL
        private Parameter[] DictToFirebaseParameters(Dictionary<string, object> param)
        {
            var index = 0;
            var parameters = new Parameter[param.Count];

            foreach (var p in param)
            {
                if (p.Value == null)
                {
                    parameters[index++] = new Parameter(p.Key, "null");
                    Crashlytics.Log($"{TAG} - DictToFirebaseParameters() -> Parameter {p.Key} has null value.");

                    continue;
                }

                parameters[index++] = p.Value switch
                {
                    int iVal => new Parameter(p.Key, iVal),
                    long lVal => new Parameter(p.Key, lVal),
                    float fVal => new Parameter(p.Key, fVal),
                    double dVal => new Parameter(p.Key, dVal),
                    string sVal => new Parameter(p.Key, sVal),
                    _ => new Parameter(p.Key, p.Value.ToString())
                };
            }

            return parameters;
        }
#endif
    }
}
