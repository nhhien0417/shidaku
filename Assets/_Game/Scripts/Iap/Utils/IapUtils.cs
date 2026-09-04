using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Iap
{
    public static class IapUtils
    {
        private static IPurchaseVerificator _purchaseVerificator;

        public static void ValidateAndLogPurchase(Product product, Action<bool> onValidated, Action<string> onError)
        {
            var verificator = GetPurchaseVerificator();
            if (verificator != null)
            {
                verificator.ValidateAndLogPurchase(product, onValidated, onError);
            }
            else
            {
                Debug.LogError("[IapUtils] No purchase verificator found. Purchase cannot be validated.");
                onValidated?.Invoke(false);
            }
        }

        private static IPurchaseVerificator GetPurchaseVerificator()
        {
            if (_purchaseVerificator == null)
            {
                _purchaseVerificator = UnityEngine.Object.FindAnyObjectByType<AppsflyerEventListenerCustom>(FindObjectsInactive.Exclude);
                if (_purchaseVerificator == null) // Currently using Appsflyer for purchase validation as default. Change if needed
                {
                    Debug.LogWarning("[IapUtils] PurchaseVerificator not found in the scene. Use AppsflyerPurchaseVerificator as default.");
                    var verificator = new GameObject("AppsflyerPurchaseVerificator");
                    _purchaseVerificator = verificator.AddComponent<AppsflyerPurchaseVerificator>();
                    UnityEngine.Object.DontDestroyOnLoad(verificator.gameObject);
                }
            }

            return _purchaseVerificator;
        }
    }
}