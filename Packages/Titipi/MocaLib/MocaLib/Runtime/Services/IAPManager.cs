using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

using Titipi.MocaLib.Runtime.Common;
using Titipi.MocaLib.Runtime.Services.Internal;

namespace Titipi.MocaLib.Runtime.Services
{
    public class IAPManager : MonoBehaviour, IDetailedStoreListener
    {
        private const string TAG = "IAPManager";

        private IStoreController _storeController;
        private IExtensionProvider _extensionProvider;

        private bool _isInitialized => _storeController != null && _extensionProvider != null;

        /// <summary>
        /// For ANDROID: 
        /// - Set OnRestorePurchases callback BEFORE calling Initialize to make sure restored purchases can be processed.
        ///
        /// For IOS:
        /// - Make sure OnRestorePurchases callback is set before calling RestorePurchases to make sure restored purchases can be processed.
        /// 
        /// This callback will be called for each restored purchase.
        /// After restore process is done, the callback will be set to null to avoid unexpected call.
        /// </summary>
        public Action<Product, bool> OnRestorePurchases;

        private IAPConfig _config = new();
        private IIapPurchaseVerificator _purchaseVerificator;

        private Action _onInitialized;
        private Action<Product> _onPurchaseSucceeded;
        private Action<string> _onPurchaseFailed;
        private Action _onStartValidatePurchase;

        private const string PLAYER_PREFS_KEY_PENDING_PURCHASE_ID = "moca_pending_iap_id";

        public void SetConfig(IAPConfig config)
        {
            _config = config;
            if (_config == null)
            {
                _config = new IAPConfig()
                {
                    UsePurchaseVerification = false
                };
            }
        }

        public void Initialize(List<(string productId, ProductType productType)> productList, Action onInitialized)
        {
            Utils.MocaLibLog(TAG, "Initialize");

            if (productList == null || productList.Count == 0) return;

            _onInitialized = onInitialized;

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (var (productId, productType) in productList)
            {
                Utils.MocaLibLog(TAG, $"    {productId} - {productType}");
                builder.AddProduct(productId, productType);
            }

            UnityPurchasing.Initialize(this, builder);
        }

        public void PurchaseProduct(string productId, Action<Product> onSuccessful, Action<string> onFailed)
        {
            if (!_isInitialized)
            {
                onFailed?.Invoke($"{TAG} not initialized");
                return;
            }

            PlayerPrefs.SetString(PLAYER_PREFS_KEY_PENDING_PURCHASE_ID, productId);
            PlayerPrefs.Save();

            _onPurchaseSucceeded = onSuccessful;
            _onPurchaseFailed = onFailed;

            if (MocaLib.Instance != null && MocaLib.Instance.AdManager != null)
                MocaLib.Instance.AdManager.AllowAppOpenAd(false);

            _storeController.InitiatePurchase(productId);

            if (MocaLib.Instance != null && MocaLib.Instance.AdManager != null)
                MocaLib.Instance.AdManager.AllowAppOpenAd(true);
        }

        public void PurchaseProduct(string productId, Action<Product> onSuccessful, Action<string> onFailed, Action onStartValidatePurchase)
        {
            if (!_isInitialized)
            {
                onFailed?.Invoke("Purchase is not ready!");
            }
            else
            {
                _onStartValidatePurchase = onStartValidatePurchase;
                PurchaseProduct(productId, onSuccessful, onFailed);
            }
        }

#if UNITY_IOS
        public void RestorePurchases()
        {
#if !UNITY_EDITOR
            if (!_isInitialized)
            {
                Utils.MocaLibLogError(TAG, "RestorePurchases called before IAP is initialized.");
                return;
            }

            var appleExtensions = _extensionProvider.GetExtension<IAppleExtensions>();
            if (appleExtensions == null)
            {
                Utils.MocaLibLogError(TAG, "RestorePurchases failed: Apple extensions are unavailable.");
                return;
            }

            appleExtensions.RestoreTransactions((result, error) => {
                if (!result)
                {
                    Utils.MocaLibLogError(TAG, $"RestorePurchases failed: {error}");
                }
                OnRestorePurchases = null; // ✅ Restore done -> set callback to null
            });
#endif
        }
#endif

        #region Get Product Info
        public decimal GetProductPrice(string productId)
        {
            return !_isInitialized ? decimal.Zero : _storeController.products.WithID(productId).metadata.localizedPrice;
        }

        public string GetProductPriceString(string productId)
        {
            return !_isInitialized ? string.Empty : _storeController.products.WithID(productId).metadata.localizedPriceString;
        }

        public string GetProductName(string productId)
        {
            return !_isInitialized ? string.Empty : _storeController.products.WithID(productId).metadata.localizedTitle;
        }

        public string GetProductDescription(string productId)
        {
            return !_isInitialized ? string.Empty : _storeController.products.WithID(productId).metadata.localizedDescription;
        }

        public string GetISOCurrencyCode(string productId)
        {
            return !_isInitialized ? string.Empty : _storeController.products.WithID(productId).metadata.isoCurrencyCode;
        }
        #endregion

        #region Fetch Additional Products
        public void FetchAdditionalProducts(Action onSuccess, Action<InitializationFailureReason, string> onFailed, ProductType productType, params string[] productIds)
        {
            if (productIds == null || productIds.Length == 0)
            {
                onFailed?.Invoke(InitializationFailureReason.NoProductsAvailable, "No product ids provided");
                return;
            }

            var hashSet = new HashSet<ProductDefinition>();
            foreach (var productId in productIds)
            {
                hashSet.Add(new ProductDefinition(productId, productType));
            }

            FetchAdditionalProducts(hashSet, onSuccess, onFailed);
        }

        public void FetchAdditionalProducts(Action onSuccess, Action<InitializationFailureReason, string> onFailed, params ProductDefinition[] products)
        {
            if (products == null || products.Length == 0)
            {
                onFailed?.Invoke(InitializationFailureReason.NoProductsAvailable, "No products provided");
                return;
            }

            var hashSet = new HashSet<ProductDefinition>(products);
            FetchAdditionalProducts(hashSet, onSuccess, onFailed);
        }

        private void FetchAdditionalProducts(HashSet<ProductDefinition> products, Action onSuccess, Action<InitializationFailureReason, string> onFailed)
        {
            _storeController.FetchAdditionalProducts(products, onSuccess, onFailed);
        }
        #endregion

        #region Verify Purchase
        public void SetPurchaseVerificator(IIapPurchaseVerificator verificator)
        {
            _purchaseVerificator = verificator;
        }

        private IEnumerator VerifyPurchase(Product product, Action<bool> onValidated, Action<string> onError)
        {
#if UNITY_EDITOR
            onValidated?.Invoke(true);
            yield break;
#endif

            if (product == null)
            {
                onError?.Invoke(IapVerificationErrorId.NullProduct);
                yield break;
            }

            // Enable check
            if (!_config.UsePurchaseVerification)
            {
                onValidated?.Invoke(true);
                yield break;
            }

            // Ignore IOS check
#if UNITY_IOS
            if (_config.IgnoreVerifyIosPurchase)
            {
                onValidated?.Invoke(true);
                yield break;
            }
#endif

            // Ignore subscription check
            if (_config.IgnoreVerifySubscription && product.definition.type == ProductType.Subscription)
            {
                onValidated?.Invoke(true);
                yield break;
            }

            // Verificator check
            if (_purchaseVerificator == null)
            {
                onError?.Invoke(IapVerificationErrorId.VerificatorNotFound);
                yield break;
            }

            _onStartValidatePurchase?.Invoke();

            // Start verify
            var waitingForVerify = true;
            var purchaseValid = false;
            var isError = false;
            var errorMessage = "";
            _purchaseVerificator.ValidatePurchase(product, iapValid =>
            {
                waitingForVerify = false;
                purchaseValid = iapValid;
            }, error =>
            {
                waitingForVerify = false;
                isError = true;
                errorMessage = error;
            });

            // Waiting for verify completed
            var timeout = _config.PurchaseVerificationTimeout;
            while (waitingForVerify && timeout > 0f)
            {
                yield return new WaitForEndOfFrame();
                timeout -= Time.deltaTime;
            }

            // Check timeout
            if (timeout <= 0f && waitingForVerify)
            {
                errorMessage = IapVerificationErrorId.Timeout;

                if (_config.ReturnSuccessOnPurchaseVerificationTimeout)
                {
                    var checkInternetTask = NetworkUtils.CheckInternetConnectionAvailable();
                    while (!checkInternetTask.IsCompleted)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                    var internetAvailable = checkInternetTask.Result;
                    isError = !internetAvailable;
                }

                purchaseValid |= (_config.ReturnSuccessOnPurchaseVerificationTimeout && !isError);
            }

            // Return result
            if (isError)
            {
                Utils.MocaLibLogError(TAG, $"VerifyPurchase -> error: {errorMessage}");
                onError?.Invoke(errorMessage);
            }
            else
            {
                onValidated?.Invoke(purchaseValid);
            }
        }
        #endregion

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            if (_onPurchaseSucceeded != null)
            {
                var product = purchaseEvent.purchasedProduct;

                if (_config.UsePurchaseVerification)
                {
                    StartCoroutine(VerifyPurchase(product, isValid =>
                    {
                        if (isValid)
                        {
                            ProcessPurchaseSuccessful(product);
                        }
                        else
                        {
                            ProcessPurchaseFailed(product, IapVerificationErrorId.InvalidPurchase);
                        }

                        _storeController.ConfirmPendingPurchase(product);

                    }, error =>
                    {
                        ProcessPurchaseFailed(product, IapVerificationErrorId.VerifyError);
                    }));

                    return PurchaseProcessingResult.Pending;
                }
                else
                {
                    ProcessPurchaseSuccessful(product);
                    return PurchaseProcessingResult.Complete;
                }
            }
            else if (OnRestorePurchases != null)
            {
                var productId = purchaseEvent.purchasedProduct?.definition.id ?? string.Empty;
                var pendingProduct = PlayerPrefs.GetString(PLAYER_PREFS_KEY_PENDING_PURCHASE_ID, string.Empty);
                var isPendingPurchase = !string.IsNullOrEmpty(pendingProduct) && pendingProduct == productId;

                OnRestorePurchases.Invoke(purchaseEvent.purchasedProduct, isPendingPurchase);

                if (isPendingPurchase)
                {
                    PlayerPrefs.SetString(PLAYER_PREFS_KEY_PENDING_PURCHASE_ID, string.Empty);
                    PlayerPrefs.Save();
                }

                return PurchaseProcessingResult.Complete;
            }

            return PurchaseProcessingResult.Pending;
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
            _extensionProvider = extensions;

            _onInitialized?.Invoke();

#if UNITY_ANDROID
            StartCoroutine(ClearRestoreCallbackAfterAutoRestore()); // ✅ Auto-restore done -> set callback to null
#endif
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            ProcessPurchaseFailed(product, failureDescription.ToString());
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            ProcessPurchaseFailed(product, failureReason.ToString());
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Utils.MocaLibLogError(TAG, $"OnInitializeFailed -> error: {error}");
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Utils.MocaLibLogError(TAG, $"OnInitializeFailed -> error: {error}, message: {message}");
        }

        private void ProcessPurchaseSuccessful(Product product)
        {
            _onPurchaseSucceeded?.Invoke(product);

            PlayerPrefs.SetString(PLAYER_PREFS_KEY_PENDING_PURCHASE_ID, string.Empty);
            PlayerPrefs.Save();

#if MOCALIB_MMP_PROVIDER_APPSFLYER && !MOCALIB_USE_APPSFLYER_PURCHASE_CONNECTOR
            MocaLib.Instance.MMPManager.LogPurchase(product);
#endif

            _onPurchaseSucceeded = null;
            _onPurchaseFailed = null;
            _onStartValidatePurchase = null;
        }

        private void ProcessPurchaseFailed(Product product, string failureReason)
        {
            _onPurchaseFailed?.Invoke(failureReason);

            PlayerPrefs.SetString(PLAYER_PREFS_KEY_PENDING_PURCHASE_ID, string.Empty);
            PlayerPrefs.Save();

            _onPurchaseSucceeded = null;
            _onPurchaseFailed = null;
            _onStartValidatePurchase = null;
        }

#if UNITY_ANDROID
        private IEnumerator ClearRestoreCallbackAfterAutoRestore()
        {
            // Wait 1 frame: Unity IAP calls ProcessPurchase for pending purchases
            // synchronously right after OnInitialized returns (same frame).
            // By next frame, all pending purchases have already been delivered.
            yield return null;
            OnRestorePurchases = null;
        }

        public List<Product> GetAllPurchasedNonConsumables()
        {
            var result = new List<Product>();

            if (!_isInitialized)
                return result;

            foreach (var product in _storeController.products.all)
            {
                if (product.hasReceipt && product.definition.type == ProductType.NonConsumable)
                {
                    result.Add(product);
                }
            }

            return result;
        }
#endif
    }
}
