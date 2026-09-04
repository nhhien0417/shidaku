// using System;
// using System.Collections.Generic;
// using Design;
// using MEC;
// using UnityEngine;
// using UnityEngine.Purchasing;
// using UnityEngine.Purchasing.Extension;
//
// namespace Iap
// {
//     public class IapManager : SingletonComponent<IapManager>, IDetailedStoreListener
//     {
//         [SerializeField] private bool _usePurchaseVerification;
//         [SerializeField] private float _purchaseVerificationTimeout = 10f;
//         [SerializeField] private bool _successOnPurchaseVerificationTimeout;
//         
//         private IStoreController _storeController;
//         private IExtensionProvider _extensionProvider;
//
//         private Action<Product> _onPurchaseSuccessfulEvent;
//         private Action<string> _onPurchaseFailedEvent;
//         private Action _onValidatingPurchaseEvent;
//         private Action<bool, string> _onRestorePurchasesEvent;
//
//         private bool IsInitialized => _storeController != null && _extensionProvider != null;
//
//         // private void Start()
//         // {
//         //     InitializeIAP();
//         // }
//
//         public void InitializeIAP()
//         {
//             if (IsInitialized)
//                 return;
//
//             var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
//
//             var iapShopItems = DesignDataHolder.Instance?.IapShopItemData.Items;
//             if (iapShopItems != null)
//             {
//                 foreach (var item in iapShopItems)
//                 {
//                     builder.AddProduct(item.GetProductId(), item.ProductType);
//
//                     var discountIds = item.GetAllDiscountProductIds();
//                     foreach (var discountId in discountIds)
//                     {
//                         builder.AddProduct(discountId, item.ProductType);
//                     }
//                 }
//             }
//
//             UnityPurchasing.Initialize(this, builder);
//         }
//
//         public void PurchaseProduct(string productId, Action<Product> onSuccessful, Action<string> onFailed, Action onValidatingPurchase = null)
//         {
//             if (!IsInitialized)
//             {
//                 onFailed?.Invoke("Purchase is not ready!");
//             }
//             else
//             {
//                 _onPurchaseSuccessfulEvent = onSuccessful;
//                 _onPurchaseFailedEvent = onFailed;
//                 _onValidatingPurchaseEvent = onValidatingPurchase;
//                 _storeController.InitiatePurchase(productId);
//             }
//         }
//
// #region Get Product Info
//         public ProductMetadata GetProductMetadata(string productId)
//         {
//             if (IsInitialized)
//             {
//                 var product = _storeController.products.WithID(productId);
//
//                 if (product is { availableToPurchase: true })
//                 {
//                     return product.metadata;
//                 }
//             }
//
//             return null;
//         }
//
//         public Product GetProduct(string productId)
//         {
//             if (IsInitialized)
//             {
//                 return _storeController.products.WithID(productId);
//             }
//
//             return null;
//         }
// #endregion
//
// #region Fetch Additional Products
//         public void FetchAdditionalProducts(Action onSuccess, Action<InitializationFailureReason, string> onFailed, ProductType productType, params string[] productIds)
//         {
//             if (productIds == null || productIds.Length == 0)
//             {
//                 onFailed?.Invoke(InitializationFailureReason.NoProductsAvailable, "No product ids provided");
//                 return;
//             }
//
//             var hashSet = new HashSet<ProductDefinition>();
//             foreach (var productId in productIds)
//             {
//                 hashSet.Add(new ProductDefinition(productId, productType));
//             }
//
//             FetchAdditionalProducts(hashSet, onSuccess, onFailed);
//         }
//
//         public void FetchAdditionalProducts(Action onSuccess, Action<InitializationFailureReason, string> onFailed, params ProductDefinition[] products)
//         {
//             if (products == null || products.Length == 0)
//             {
//                 onFailed?.Invoke(InitializationFailureReason.NoProductsAvailable, "No product ids provided");
//                 return;
//             }
//
//             var hashSet = new HashSet<ProductDefinition>(products);
//             FetchAdditionalProducts(hashSet, onSuccess, onFailed);
//         }
//
//         public void FetchAdditionalProducts(HashSet<ProductDefinition> products, Action onSuccess, Action<InitializationFailureReason, string> onFailed)
//         {
//             _storeController.FetchAdditionalProducts(products, onSuccess, onFailed);
//         }
// #endregion
//
// #region Restore Products
//         public void RestoreProducts(Action<bool, string> onCompleted)
//         {
//             if (IsInitialized == false)
//             {
//                 onCompleted?.Invoke(false, "Purchase is not ready! Please try again later!");
//                 return;
//             }
//
//             _onRestorePurchasesEvent = onCompleted;
// #if UNITY_EDITOR
//             onCompleted?.Invoke(true, "Can not restore purchases in editor!");
// #elif UNITY_IOS
//         _extensionProvider.GetExtension<IAppleExtensions>().RestoreTransactions(OnRestore);
// #elif UNITY_ANDROID
//         _extensionProvider.GetExtension<IGooglePlayStoreExtensions>().RestoreTransactions(OnRestore);
// #endif
//         }
//
//         private void OnRestore(bool success, string error)
//         {
//             if (success)
//             {
//                 // This does not mean anything was restored,
//                 // merely that the restoration process succeeded.
//
//                 // foreach (var product in _storeController.products.all)
//                 // {
//                 //     if (product.hasReceipt)
//                 //     {
//                 //         if (string.Equals(product.definition.id, IAPDataManager.RemoveAds.Id))
//                 //         {
//                 //             IAPDataManager.OnBuyNoAdsSuccessful();
//                 //         }
//                 //     }
//                 // }
//
//                 Debug.Log($"[IapManager] Restore Successful");
//             }
//             else
//             {
//                 // Restoration failed.
//                 Debug.Log($"[IapManager] Restore Failed with error: {error}");
//             }
//
//             if (_onRestorePurchasesEvent != null)
//             {
//                 _onRestorePurchasesEvent.Invoke(success, error);
//                 _onRestorePurchasesEvent = null;
//             }
//         }
//         
//         private void CheckAndRestoreNonConsumables()
//         {
// #if UNITY_ANDROID
//             if (!IsInitialized)
//                 return;
//             
//             var iapShopItems = DesignDataHolder.Instance?.IapShopItemData.Items;
//             if (iapShopItems != null)
//             {
//                 foreach (var item in iapShopItems)
//                 {
//                     if (item.ProductType == ProductType.NonConsumable)
//                     {
//                         var productList = new List<Product>();
//                         
//                         var product = _storeController.products.WithID(item.GetProductId());
//                         if (product != null)
//                             productList.Add(product);
//
//                         foreach (var discountId in item.GetAllDiscountProductIds())
//                         {
//                             product = _storeController.products.WithID(discountId);
//                             if (product != null)
//                                 productList.Add(product);
//                         }
//
//                         foreach (var pd in productList)
//                         {
//                             if (pd.hasReceipt)
//                             {
//                                 item.RestorePurchase();
//                                 break;
//                             }
//                         }
//                     }
//                 }
//             }
// #endif
//         }
// #endregion
//
// #region Purchase Callbacks
//
//         private void OnPurchaseSuccessful(Product product)
//         {
//             if (_onPurchaseSuccessfulEvent != null)
//             {
//                 _onPurchaseSuccessfulEvent.Invoke(product);
//                 _onPurchaseSuccessfulEvent = null;
//             }
//         }
//
//         private void OnPurchaseFailed(string reason)
//         {
//             if (_onPurchaseFailedEvent != null)
//             {
//                 _onPurchaseFailedEvent.Invoke(reason);
//                 _onPurchaseFailedEvent = null;
//             }
//         }
//
//         private IEnumerator<float> VerifyPurchase(Product product, Action<bool> onValidated, Action<string> onError)
//         {
//             _onValidatingPurchaseEvent?.Invoke();
//             
//             var verifyCompleted = true;
//             var isValidPurchase = false;
//             var isError = false;
//             var errorMessage = "";
//
//             if (product != null)
//             {
//                 verifyCompleted = false;
//                 IapUtils.ValidateAndLogPurchase(product, isValid =>
//                 {
//                     verifyCompleted = true;
//                     isValidPurchase = isValid;
//                 }, error =>
//                 {
//                     isError = true;
//                     errorMessage = error;
//                     verifyCompleted = true;
//                     isValidPurchase = false;
//                 });
//             }
//
//             var timeOut = _purchaseVerificationTimeout;
//             while (!verifyCompleted && timeOut > 0f)
//             {
//                 yield return Timing.WaitForOneFrame;
//                 timeOut -= Time.deltaTime;
//             }
//             
//             if (timeOut <= 0f && !verifyCompleted)
//             {
//                 //Debug.LogWarning($"[IapManager] Purchase verification timed out for product {product.definition.id}");
//                 errorMessage = "Purchase verification timed out";
//                 
//                 if (_successOnPurchaseVerificationTimeout)
//                 {
//                     var checkInternetTask = NetworkUtils.CheckInternetConnectionAvailable();
//                     while (!checkInternetTask.IsCompleted)
//                     {
//                         yield return Timing.WaitForOneFrame;
//                     }
//
//                     isError = !checkInternetTask.Result;
//                 }
//                 
//                 isValidPurchase |= _successOnPurchaseVerificationTimeout;
//             }
//
//             if (isError)
//             {
//                 Debug.LogError($"[IapManager] Purchase verification error for subscription product {product.definition.id}: {errorMessage}");
//             }
//
//         #if UNITY_IOS // bypass verification on iOS
//             isValidPurchase = true;
//             isError = false;
//         #endif
//
//             // For subscription products, we may want to bypass verification to avoid issues with receipt validation
//             // which can be unreliable in some cases. This is a decision that should be made based on your specific requirements and risk tolerance.
//             if (product.definition.type == ProductType.Subscription)
//             {
//                 isValidPurchase = true;
//                 isError = false;
//             }
//
//             if (isError)
//             {
//                 onError?.Invoke(errorMessage);
//             }
//             else
//             {
//                 onValidated?.Invoke(isValidPurchase);
//             }
//         }
//
// #endregion
//
// #region IStoreListener
//
//         public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
//         {
//             _storeController = controller;
//             _extensionProvider = extensions;
//             CheckAndRestoreNonConsumables();
//             Debug.Log("[IapManager] Initialize successful");
//         }
//
//         public void OnInitializeFailed(InitializationFailureReason error)
//         {
//             Debug.Log($"[IapManager] Initialize failed: {error}");
//         }
//
//         public void OnInitializeFailed(InitializationFailureReason error, string? message)
//         {
//             Debug.Log($"[IapManager] Initialize failed: \nError:{error} \nMessage:{message}");
//         }
//
//         public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
//         {
//         #if UNITY_EDITOR
//             OnPurchaseSuccessful(purchaseEvent.purchasedProduct);
//             return PurchaseProcessingResult.Complete;
//         #else
//             if (_usePurchaseVerification)
//             {
//                 Timing.RunCoroutine(VerifyPurchase(purchaseEvent.purchasedProduct, isValid =>
//                 {
//                     if (isValid)
//                     {
//                         OnPurchaseSuccessful(purchaseEvent.purchasedProduct);
//                     }
//                     else
//                     {
//                         OnPurchaseFailed(VerificationResultError.InvalidPurchase);
//                     }
//                     _storeController.ConfirmPendingPurchase(purchaseEvent.purchasedProduct);
//
//                 }, error =>
//                 {
//                     OnPurchaseFailed(VerificationResultError.VerifyError);
//                 }));
//
//                 return PurchaseProcessingResult.Pending;
//             }
//             else
//             {
//                 OnPurchaseSuccessful(purchaseEvent.purchasedProduct);
//                 return PurchaseProcessingResult.Complete;
//             }
//         #endif
//         }
//
//         public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
//         {
//             Debug.Log($"[IapManager] Purchasing failed: {failureReason}");
//             OnPurchaseFailed(failureReason.ToString());
//         }
//
//         public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
//         {
//             Debug.Log($"[IapManager] Purchasing failed: {failureDescription.message}");
//             OnPurchaseFailed(failureDescription.message);
//         }
//
// #endregion // IStoreListener
//
//         public abstract class VerificationResultError
//         {
//             public const string VerifyError = "verify_error";
//             public const string InvalidPurchase = "invalid_purchase";
//         }
//     }
// }