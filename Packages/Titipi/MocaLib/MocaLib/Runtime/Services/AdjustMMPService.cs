#if MOCALIB_MMP_PROVIDER_ADJUST

using System.Collections.Generic;
using UnityEngine.Purchasing;

using AdjustSdk;

using Titipi.MocaLib.Runtime.Services.Internal;

namespace Titipi.MocaLib.Runtime.Services
{
    public class AdjustMMPService : MMPService
    {
        private const string TAG = "AdjustMMPService";

        private string AppToken;

        public void Initialize(MMPConfig mmpConfig)
        {
            Utils.MocLibLog(TAG, "Initialize");

#if UNITY_IOS
            AppToken = mmpConfig.AppTokenIOS;
#elif UNITY_ANDROID
            AppToken = mmpConfig.AppTokenAndroid;
#endif

            var config = new AdjustConfig(AppToken, AdjustEnvironment.Production)
            {
                LogLevel = AdjustLogLevel.Verbose,
                IsSendingInBackgroundEnabled = true,
                IsDeferredDeeplinkOpeningEnabled = true
            };

            Adjust.InitSdk(config);
        }

        public void LogEvent(string eventName)
        {
            var adjustEvent = new AdjustEvent(eventName);
            Adjust.TrackEvent(adjustEvent);
        }

        public void LogEvent(string eventName, Dictionary<string, string> data)
        {
            var adjustEvent = new AdjustEvent(eventName);

            foreach (var kvp in data)
            {
                adjustEvent.AddCallbackParameter(kvp.Key, kvp.Value);
            }

            Adjust.TrackEvent(adjustEvent);
        }

        public void LogPurchaseToAdjust(Product product, string adjustToken)
        {
            var adjustEvent = new AdjustEvent(adjustToken);

            var price = (double) product.metadata.localizedPrice;
            var currencyCode = product.metadata.isoCurrencyCode;

            adjustEvent.ProductId = product.definition.id;
            adjustEvent.SetRevenue(price, currencyCode);

#if UNITY_ANDROID
            var receiptDict = (Dictionary<string, object>) MiniJson.JsonDecode(product.receipt);
            var payloadJson = (null != receiptDict && receiptDict.ContainsKey("Payload"))
                ? (string) receiptDict["Payload"]
                : "";
            var jsonDetailsDict = (!string.IsNullOrEmpty(payloadJson))
                ? (Dictionary<string, object>) MiniJson.JsonDecode(payloadJson)
                : null;
            var json = (jsonDetailsDict != null && jsonDetailsDict.ContainsKey("json"))
                ? (string) jsonDetailsDict["json"]
                : "";
            var gpDetailsDict = (!string.IsNullOrEmpty(json))
                ? (Dictionary<string, object>) MiniJson.JsonDecode(json)
                : null;
            var purchaseToken = (null != gpDetailsDict && gpDetailsDict.ContainsKey("purchaseToken"))
                ? (string) gpDetailsDict["purchaseToken"]
                : "";

            adjustEvent.PurchaseToken = purchaseToken;

            Adjust.VerifyAndTrackPlayStorePurchase(adjustEvent, verificationResult =>
            {
                // Debug.Log("Verification status: " + verificationResult.VerificationStatus);
                // Debug.Log("Code: " + verificationResult.Code);
                // Debug.Log("Message: " + verificationResult.Message);
            });

#elif UNITY_IOS
            adjustEvent.TransactionId = product.transactionID;

            Adjust.VerifyAndTrackAppStorePurchase(adjustEvent, verificationResult =>
            {
                // Debug.Log("Verification status: " + verificationResult.VerificationStatus);
                // Debug.Log("Code: " + verificationResult.Code);
                // Debug.Log("Message: " + verificationResult.Message);
            });
#endif
        }

        public void LogPurchaseToAppsFlyer(Product product, string googlePublicKey)
        {
            throw new System.NotImplementedException();
        }
    }
}

#endif // MOCALIB_MMP_PROVIDER_ADJUST
