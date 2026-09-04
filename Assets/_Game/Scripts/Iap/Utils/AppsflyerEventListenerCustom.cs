using System;
using System.Collections.Generic;
using AppsFlyerSDK;
using CostCenter.RemoteConfig;
using Iap;
using Titipi.MocaLib.Runtime.Services;
using Titipi.MocaLib.Runtime.Services.Internal;
using UnityEngine;
using UnityEngine.Purchasing;

public class AppsflyerEventListenerCustom : AppsflyerEventListener, IPurchaseVerificator, IIapPurchaseVerificator
{
    private Dictionary<string, PurchaseValidationResult> _validationResults = new ();
    private Dictionary<string, PurchaseValidationRequest> _validationRequests = new ();

    private void Start()
    {
        MocaLib.Instance?.IAPManager.SetPurchaseVerificator(this);
    }

    public override void onConversionDataSuccess(string conversionData)
    {
        var dict = AppsFlyer.CallbackStringToDictionary(conversionData);
        if (dict != null)
            CCRemoteConfig.instance?.OnConversionDataSuccess(dict);

        Debug.Log($"[AppsflyerEventListenerCustom]: onConversionDataSuccess: {conversionData}");
    }

    public override void didReceivePurchaseRevenueValidationInfo(string validationInfo)
    {
        // Debug.Log($"[PurchaseVerificator]: {validationInfo}");
#if UNITY_IOS
        base.didReceivePurchaseRevenueValidationInfo(validationInfo);
#else
        var dict = AFMiniJSON.Json.Deserialize(validationInfo) as Dictionary<string, object>;
        if (dict != null)
        {
            var token = dict.ContainsKey("token") ? dict["token"].ToString() : "";
            var validated = false;

            if (dict.ContainsKey("productPurchase") || dict.ContainsKey("subscriptionPurchase"))
            {
                Debug.Log("[PurchaseVerificator] in-app purchase validated");
                validated = true;
            }

            if (_validationRequests.TryGetValue(token, out var request))
            {
                request.OnValidated?.Invoke(validated);
                _validationRequests.Remove(token);
            }
            else
            {
                _validationResults[token] = new PurchaseValidationResult
                {
                    TransactionId = token,
                    IsValid = validated,
                    ValidationInfo = validationInfo
                };
            }
        }
#endif
    }

    public void ValidateAndLogPurchase(Product product, Action<bool> onValidated, Action<string> onError)
    {
#if UNITY_IOS
        onValidated?.Invoke(true);
#else
        Debug.Log($"[PurchaseVerificator] Request: {product.transactionID}");
        var transactionId = product.transactionID;
        if (_validationResults.TryGetValue(transactionId, out var result))
        {
            onValidated?.Invoke(result.IsValid);
            _validationResults.Remove(transactionId);
        }
        else
        {
            _validationRequests[transactionId] = new PurchaseValidationRequest
            {
                TransactionId = transactionId,
                OnValidated = onValidated,
                OnError = onError
            };
        }
#endif
    }

    public void ValidatePurchase(Product product, Action<bool> onValidated, Action<string> onError)
    {
        ValidateAndLogPurchase(product, onValidated, onError);
    }

    private struct PurchaseValidationResult
    {
        public string TransactionId;
        public bool IsValid;
        public string ValidationInfo;
    }

    private struct PurchaseValidationRequest
    {
        public string TransactionId;
        public Action<bool> OnValidated;
        public Action<string> OnError;
    }
}
