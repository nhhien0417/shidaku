using System;
using System.Collections.Generic;
using AppsFlyerSDK;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Iap
{
    public class AppsflyerPurchaseVerificator : MonoBehaviour, IPurchaseVerificator, IAppsFlyerValidateAndLog
    {
        private Action<bool> _onValidated;
        private Action<string> _onError;

        public void ValidateAndLogPurchase(Product product, Action<bool> onValidated, Action<string> onError)
        {
            _onValidated = onValidated;
            _onError = onError;

            var price = product.metadata.localizedPrice.ToString(); // Ex: "1.99"
            var currency = product.metadata.isoCurrencyCode;        // Ex: "USD"
            var productId = product.definition.id;

        #if UNITY_IOS
            // (Optional) Enable this line if you are testing in Apple's Sandbox environment
            // AppsFlyeriOS.setUseReceiptValidationSandbox(true);
            var transactionId = product.transactionID;
            var purchaseType = product.definition.type == ProductType.Subscription ? AFSDKPurchaseType.Subscription : AFSDKPurchaseType.OneTimePurchase;
            var detailsIOS = AFSDKPurchaseDetailsIOS.Init(productId, transactionId, purchaseType);

            Debug.Log($"[AppsflyerPurchaseVerificator] Start verify purchase iOS: transactionId={transactionId}");

            AppsFlyer.validateAndSendInAppPurchase(detailsIOS, null, this);

        #elif UNITY_ANDROID
            var purchaseToken = ExtractAndroidPurchaseToken(product.receipt);
            var purchaseType = product.definition.type == ProductType.Subscription ? AFPurchaseType.Subscription : AFPurchaseType.OneTimePurchase;
            var detailsAndroid = new AFPurchaseDetailsAndroid(purchaseType, purchaseToken, productId);

            Debug.Log($"[AppsflyerPurchaseVerificator] Start verify purchase Android: purchaseToken={purchaseToken}");

            AppsFlyer.validateAndSendInAppPurchase(detailsAndroid, null, this);
        #endif

            onValidated?.Invoke(true); // Ignore verification result for now due to dashboard not configured yet.
        }

        private string ExtractAndroidPurchaseToken(string unityIapReceipt)
        {
            try {
                var receiptDict = (Dictionary<string, object>) MiniJson.JsonDecode(unityIapReceipt);
                var payloadJson = (null != receiptDict && receiptDict.ContainsKey("Payload")) ? (string) receiptDict["Payload"] : "";
                var jsonDetailsDict = (!string.IsNullOrEmpty(payloadJson)) ? (Dictionary<string, object>) MiniJson.JsonDecode(payloadJson) : null;
                var json = (jsonDetailsDict != null && jsonDetailsDict.ContainsKey("json")) ? (string) jsonDetailsDict["json"] : "";
                var gpDetailsDict = (!string.IsNullOrEmpty(json)) ? (Dictionary<string, object>) MiniJson.JsonDecode(json) : null;
                var purchaseToken = (null != gpDetailsDict && gpDetailsDict.ContainsKey("purchaseToken")) ? (string) gpDetailsDict["purchaseToken"] : "";

                return purchaseToken;

            }
            catch (Exception e)
            {

                Debug.LogError("[AppsflyerPurchaseVerificator] ExtractAndroidPurchaseToken: " + e.Message);
                return "";
            }
        }

        public void onValidateAndLogComplete(string result)
        {
            Debug.Log("[AppsflyerPurchaseVerificator] Validation Result: " + result);

            // try
            // {
            //     var resultDict = AppsFlyer.CallbackStringToDictionary(result) as Dictionary<string, object>;

            //     if (resultDict != null)
            //     {
            //         var isValid = false;
            //         if (resultDict.TryGetValue("result", out var resultValue))
            //         {
            //             isValid = resultValue.ToString().ToLower() == "true";
            //         }

            //         _onValidated?.Invoke(isValid);
            //     }
            // }
            // catch (System.Exception e)
            // {
            //     Debug.LogError("[AppsflyerPurchaseVerificator] onValidateAndLogComplete: " + e.Message);
            // }

            // AppsFlyer.AFLog("onValidateAndLogComplete", result);
            // Dictionary<string, object> validateAndLogDataDictionary = AppsFlyer.CallbackStringToDictionary(result);
            // Debug.Log($"AppsFlyer validation result: {result}");
            // _onValidated?.Invoke(true);
        }

        public void onValidateAndLogFailure(string error)
        {
            // AppsFlyer.AFLog("onValidateAndLogFailure", error);
            Debug.LogError("[AppsflyerPurchaseVerificator] onValidateAndLogFailure: " + error);
            //Dictionary<string, object> validateAndLogErrorDictionary = AppsFlyer.CallbackStringToDictionary(error);
            // _onError?.Invoke(""); // TODO: Extract error message from AppsFlyer response and pass it to the callback
        }
    }
}