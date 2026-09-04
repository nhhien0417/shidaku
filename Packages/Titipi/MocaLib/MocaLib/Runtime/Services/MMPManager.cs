using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

using Titipi.MocaLib.Runtime.Common;
using Titipi.MocaLib.Runtime.Services.Internal;

namespace Titipi.MocaLib.Runtime.Services
{
    public class MMPManager : MonoBehaviour
    {
        private const string TAG = "MMPManager";

        private MMPConfig _mmpConfig;
        private MMPService _mmpService;

        private bool _isInitialized;

        public void Initialize(MMPConfig config)
        {
#if UNITY_WEBGL
            return;
#endif
            
            Utils.MocaLibLog(TAG, "Initialize");

            MMPService service = null;
            _mmpConfig = config;

#if MOCALIB_MMP_PROVIDER_ADJUST
            service = new AdjustMMPService();
#elif MOCALIB_MMP_PROVIDER_APPSFLYER
            service = new AppsFlyerMMPService();
#endif

            if (service == null)
            {
                Utils.MocaLibLogWarning(TAG, "MMP service not defined. Please define one; or disregard this warning if you don't want to use an MMP service.");
            }
            else
            {
                if (_mmpConfig == null)
                {
                    throw new NullReferenceException("[Titipi.MocaLib] [MMPManager] MMPConfig is null!");
                }

                _mmpService = service;
                _mmpService.Initialize(_mmpConfig);

                _isInitialized = true;
            }
        }

        public void LogEvent(string eventName)
        {
#if UNITY_WEBGL
            return;
#endif

            if (!_isInitialized) return;

#if UNITY_EDITOR
            Utils.MocaLibLog(TAG, $"LogEvent: {eventName}");
#else
            _mmpService.LogEvent(eventName);
#endif
        }

        public void LogEvent(string eventName, Dictionary<string, string> data)
        {
#if UNITY_WEBGL
            return;
#endif

            if (!_isInitialized) return;

#if UNITY_EDITOR
            Utils.MocaLibLog(TAG, $"LogEvent: {eventName}, with params:");
            foreach (var kvp in data)
            {
                Utils.MocaLibLog(TAG, $"    {kvp.Key}: {kvp.Value}");
            }
#else
            _mmpService.LogEvent(eventName, data);
#endif
        }

        public void LogAdRevenue(AdImpressionData adImpressionData)
        {
#if UNITY_WEBGL
            return;
#endif

            if (!_isInitialized) return;

#if MOCALIB_MMP_PROVIDER_ADJUST
            _mmpService.LogAdRevenueToAdjust(adImpressionData);
#elif MOCALIB_MMP_PROVIDER_APPSFLYER
            _mmpService.LogAdRevenueToAppsFlyer(adImpressionData);
#endif
        }

#if MOCALIB_MMP_PROVIDER_ADJUST
        public void LogPurchase(Product product, string adjustToken)
        {
#if UNITY_WEBGL
            return;
#endif

            if (!_isInitialized) return;

            _mmpService.LogPurchaseToAdjust(product, adjustToken);
        }
#elif MOCALIB_MMP_PROVIDER_APPSFLYER
        public void LogPurchase(Product product)
        {
#if UNITY_WEBGL
            return;
#endif

            if (!_isInitialized) return;

            _mmpService.LogPurchaseToAppsFlyer(product, _mmpConfig.GooglePublicKey);
        }
#endif
    }
}
