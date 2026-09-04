using System.Collections.Generic;
using UnityEngine;
using Titipi.MocaLib.Runtime.Common;

#if MOCALIB_MMP_PROVIDER_APPSFLYER
using AppsFlyerSDK;
#endif

namespace Titipi.MocaLib.Runtime.Services.Internal
{
    public class AppsflyerEventListener : MonoBehaviour
        #if MOCALIB_MMP_PROVIDER_APPSFLYER
        ,IAppsFlyerConversionData
        ,IAppsFlyerPurchaseValidation
        ,IAppsFlyerPurchaseRevenueDataSource
        ,IAppsFlyerPurchaseRevenueDataSourceStoreKit2
        #endif
    {
        protected const string TAG = "AppsflyerEventListener";
        
#if MOCALIB_MMP_PROVIDER_APPSFLYER
        public virtual void onConversionDataSuccess(string conversionData)
        {
            
        }

        public virtual void onConversionDataFail(string error)
        {
            
        }

        public virtual void onAppOpenAttribution(string attributionData)
        {
            
        }

        public virtual void onAppOpenAttributionFailure(string error)
        {
            
        }

        public virtual void didReceivePurchaseRevenueValidationInfo(string validationInfo)
        {
            Utils.MocaLibLog(TAG, "Validate iap result: " + validationInfo);
        }

        public virtual void didReceivePurchaseRevenueError(string error)
        {
            Utils.MocaLibLogError(TAG, "Validate iap result get error: " + error);
        }

        public virtual Dictionary<string, object> PurchaseRevenueAdditionalParametersForProducts(HashSet<object> products, HashSet<object> transactions)
        {
            return new();
        }

        public virtual Dictionary<string, object> PurchaseRevenueAdditionalParametersStoreKit2ForProducts(HashSet<object> products, HashSet<object> transactions)
        {
            return new();
        }
#endif
    }
}