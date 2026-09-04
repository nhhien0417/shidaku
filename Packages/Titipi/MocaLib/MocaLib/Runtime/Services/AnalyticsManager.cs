using System.Collections.Generic;
using UnityEngine;

using Titipi.MocaLib.Runtime.Common;
using Titipi.MocaLib.Runtime.Services.Internal;
using UnityEngine.Purchasing;

namespace Titipi.MocaLib.Runtime.Services
{
    public class AnalyticsManager : MonoBehaviour, IAnalyticsService
    {
        private const string TAG = "AnalyticsManager";

        private readonly List<IAnalyticsService> _services = new();

        public void Initialize()
        {
#if !UNITY_WEBGL
            Utils.MocaLibLog(TAG, "Initialize");

            foreach (var service in _services)
            {
                service.Initialize();
            }
#endif
        }

        public void LogEvent(string eventName)
        {
#if UNITY_WEBGL
            return;
#endif

#if UNITY_EDITOR
            Utils.MocaLibLog(TAG, $"LogEvent: {eventName}");
#else
            foreach (var service in _services)
            {
                service.LogEvent(eventName);
            }
#endif
        }

        public void LogEvent(string eventName, string parameterName, string parameterValue)
        {
#if UNITY_WEBGL
            return;
#endif

#if UNITY_EDITOR
            Utils.MocaLibLog(TAG, $"LogEvent: {eventName}, param : {parameterName}, value : {parameterValue}");
#else
            foreach (var service in _services)
            {
                service.LogEvent(eventName, parameterName, parameterValue);
            }
#endif
        }

        public void LogEvent(string eventName, Dictionary<string, object> data)
        {
#if UNITY_WEBGL
            return;
#endif

#if UNITY_EDITOR
            Utils.MocaLibLog(TAG, $"LogEvent: {eventName}, with params:");
            foreach (var kvp in data)
            {
                Utils.MocaLibLog(TAG, $"    {kvp.Key}: {kvp.Value}");
            }
#else
            foreach (var service in _services)
            {
                service.LogEvent(eventName, data);
            }
#endif
        }

        public void LogUserProperty(string key, string value)
        {
#if UNITY_WEBGL
            return;
#endif

#if UNITY_EDITOR
            Utils.MocaLibLog(TAG, $"LogUserProperty: {key}: {value}");
#else
            foreach (var service in _services)
            {
                service.LogUserProperty(key, value);
            }
#endif
        }

        public void LogPurchase(string playMode, int level, Product product, string category)
        {
#if UNITY_WEBGL
            return;
#endif

#if UNITY_EDITOR
            Utils.MocaLibLog(TAG, $"LogPurchase: playMode = {playMode}, level = {level}, productId = {product.definition.id}, amount = {product.metadata.localizedPrice}, currency = {product.metadata.isoCurrencyCode}, category = {category}");
#else
            foreach (var service in _services)
            {
                service.LogPurchase(playMode, level, product, category);
            }
#endif
        }

        public void LogFirstOpen()
        {
#if UNITY_WEBGL
            return;
#endif

#if UNITY_EDITOR
            Utils.MocaLibLog(TAG, "LogFirstOpen");
#else
            foreach (var service in _services)
            {
                service.LogFirstOpen();
            }
#endif
        }

        public void LogAppOpen()
        {
#if UNITY_WEBGL
            return;
#endif

#if UNITY_EDITOR
            Utils.MocaLibLog(TAG, "LogAppOpen");
#else
            foreach (var service in _services)
            {
                service.LogAppOpen();
            }
#endif
        }

        public void LogAdRevenue(string playMode, int level, string location, AdImpressionData adImpressionData, Dictionary<string, string> customParamsDict = null)
        {
#if UNITY_EDITOR
            Utils.MocaLibLog(TAG, $"LogAdRevenue: playMode = {playMode}, level = {level}, location = {location}");
#else
            foreach (var service in _services)
            {
                service.LogAdRevenue(playMode, level, location, adImpressionData, customParamsDict);
            }
#endif
        }

        public void LogSourceEvent(string currency, int amount, string itemType, string itemId)
        {
#if UNITY_EDITOR
            Utils.MocaLibLog(TAG, $"LogSourceEvent: currency = {currency}, amount = {amount}, itemType = {itemType}, itemId = {itemId}");
#else
            foreach (var service in _services)
            {
                service.LogSourceEvent(currency, amount, itemType, itemId);
            }
#endif
        }

        public void LogSinkEvent(string currency, int amount, string itemType, string itemId)
        {
#if UNITY_EDITOR
            Utils.MocaLibLog(TAG, $"LogSinkEvent: currency = {currency}, amount = {amount}, itemType = {itemType}, itemId = {itemId}");
#else
            foreach (var service in _services)
            {
                service.LogSinkEvent(currency, amount, itemType, itemId);
            }
#endif
        }

        public void AddService(IAnalyticsService service)
        {
            _services.Add(service);
        }
    }
}
