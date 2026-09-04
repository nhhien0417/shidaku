#if MOCALIB_MMP_PROVIDER_APPSFLYER

using System.Collections.Generic;
using System.Globalization;
using UnityEngine.Purchasing;

using AppsFlyerSDK;

using Titipi.MocaLib.Runtime.Services.Internal;
using Titipi.MocaLib.Runtime.Common;

namespace Titipi.MocaLib.Runtime.Services
{
    public class AppsFlyerMMPService : MMPService
    {
        private const string TAG = "AppsFlyerMMPService";

        public void Initialize(MMPConfig mmpConfig)
        {
            Utils.MocaLibLog(TAG, "Initialize");

            AppsflyerEventListener listener = null;
            if (mmpConfig.AppsflyerEventListener == null)
            {
                Utils.MocaLibLogWarning(TAG, "AppsflyerEventListener not found. Using default listener.");
                var o = new UnityEngine.GameObject("AppsflyerEventListener");
                listener = o.AddComponent<AppsflyerEventListener>();
                UnityEngine.Object.DontDestroyOnLoad(listener.gameObject);
            }
            else
            {
                listener = UnityEngine.Object.Instantiate(mmpConfig.AppsflyerEventListener);
                UnityEngine.Object.DontDestroyOnLoad(listener.gameObject);
                Utils.MocaLibLog(TAG, "AppsflyerEventListener found. Instance created.");
            }

            AppsFlyer.setIsDebug(mmpConfig.EnableDebug);
            AppsFlyer.initSDK(mmpConfig.DevKey, mmpConfig.AppIdIOS, listener);

#if MOCALIB_USE_APPSFLYER_PURCHASE_CONNECTOR

            AppsFlyerPurchaseConnector.init(listener, Store.GOOGLE);
            AppsFlyerPurchaseConnector.setStoreKitVersion(StoreKitVersion.SK2);
            if (mmpConfig.EnableSandboxTest)
                AppsFlyerPurchaseConnector.setIsSandbox(true);

            AppsFlyerPurchaseConnector.setAutoLogPurchaseRevenue(
                AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsAutoRenewableSubscriptions,
                AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsInAppPurchases
            );

            AppsFlyerPurchaseConnector.setPurchaseRevenueValidationListeners(true);
            AppsFlyerPurchaseConnector.setPurchaseRevenueDataSource(listener);
            AppsFlyerPurchaseConnector.setPurchaseRevenueDataSourceStoreKit2(listener);

            AppsFlyerPurchaseConnector.build();
            AppsFlyerPurchaseConnector.startObservingTransactions();

#endif

            AppsFlyer.startSDK();
        }

        public void LogEvent(string eventName)
        {
            AppsFlyer.sendEvent(eventName, null);
        }

        public void LogEvent(string eventName, Dictionary<string, string> data)
        {
            AppsFlyer.sendEvent(eventName, data);
        }

        public void LogAdRevenueToAdjust(AdImpressionData adImpressionData)
        {
            throw new System.NotImplementedException();
        }

        public void LogAdRevenueToAppsFlyer(AdImpressionData adImpressionData)
        {
#if MOCALIB_AD_PROVIDER_APPLOVIN
            Dictionary<string, string> additionalParams = new ()
            {
                { AdRevenueScheme.AD_UNIT, adImpressionData.AdUnitId },
                { AdRevenueScheme.AD_TYPE, adImpressionData.AdFormat },
            };

            var logRevenue = new AFAdRevenueData("monetizationNetworkEx", MediationNetwork.ApplovinMax, "USD", adImpressionData.Value);
            AppsFlyer.logAdRevenue(logRevenue, additionalParams);
#endif
        }

        public void LogPurchaseToAppsFlyer(Product product, string googlePublicKey)
        {
            var currency = product.metadata.isoCurrencyCode;
            var price = (float) product.metadata.localizedPrice;

#if UNITY_ANDROID
            var receiptToJson = (Dictionary<string, object>) AFMiniJSON.Json.Deserialize(product.receipt);
            var receiptPayload = (Dictionary<string, object>) AFMiniJSON.Json.Deserialize((string) receiptToJson["Payload"]);

            var purchaseData = (string) receiptPayload["json"];
            var signature = (string) receiptPayload["signature"];

            new AppsFlyerAndroid().validateAndSendInAppPurchase(googlePublicKey, signature, purchaseData,
                price.ToString(CultureInfo.InvariantCulture), currency, null, null);
#elif UNITY_IOS
            var prodID = product.definition.id;
            var receipt = product.receipt;
            var transactionID = product.transactionID;

            new AppsFlyeriOS().validateAndSendInAppPurchase(prodID, price.ToString(CultureInfo.InvariantCulture), currency, transactionID, null, null);
#endif
        }

        public void LogPurchaseToAdjust(Product product, string adjustToken)
        {
            throw new System.NotImplementedException();
        }
    }
}

#endif // MOCALIB_MMP_PROVIDER_APPSFLYER
