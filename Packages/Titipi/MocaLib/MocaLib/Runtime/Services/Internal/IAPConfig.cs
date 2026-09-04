using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Titipi.MocaLib.Runtime.Services.Internal
{
    [Serializable]
    public class IAPConfig
    {
        public bool UsePurchaseVerification;
        
        [ShowIf("UsePurchaseVerification", true)]
        public float PurchaseVerificationTimeout = 10f;
        
        [ShowIf("UsePurchaseVerification", true)]
        public bool ReturnSuccessOnPurchaseVerificationTimeout;
        
        [ShowIf("UsePurchaseVerification", true)]
        public bool IgnoreVerifyIosPurchase = true;
        
        [ShowIf("UsePurchaseVerification", true)]
        public bool IgnoreVerifySubscription = true;

        [HideInInspector] 
        public Action OnVerifyPendingPurchaseStart;
        [HideInInspector] 
        public Action<bool, string> OnVerifyPendingPurchaseComplete;
    }
    
    public interface IIapPurchaseVerificator
    {
        public void ValidatePurchase(Product product, Action<bool> onValidated, Action<string> onError);
    }
    
    public abstract class IapVerificationErrorId
    {
        public const string VerifyError = "verify_error";
        public const string InvalidPurchase = "invalid_purchase";
        public const string NullProduct = "null_product";
        public const string Timeout = "timeout";
        public const string VerificatorNotFound = "verificator_not_found";
    }
}