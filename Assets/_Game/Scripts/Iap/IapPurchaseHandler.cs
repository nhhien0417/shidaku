using System;
using System.Globalization;
using Design;
using Design.Ids;
using Design.Structures;
using Titipi.MocaLib.Runtime.Services;
using Titipi.MocaLib.Runtime.Services.Internal;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Custom;
using UserDataPack;

namespace Iap
{
    public class PurchaseHandler
    {
        public static void CheckUpdateSubscriptions()
        {
            // var designData = DesignData.Instance;
            // var userData = UserData.Instance;
            // if (designData.SubscriptionData != null)
            // {
            //     var hasReward = false;
            //     foreach (var subscription in designData.SubscriptionData.Items)
            //     {
            //         var subsUpdated = false;
            //         var product = IAPManager.Instance.GetProduct(subscription.WeeklyProductId.GetProductId());
            //         if (product is { hasReceipt: true })
            //         {
            //             var subscriptionManager = new SubscriptionManagerCustom(product, null);
            //             var info = subscriptionManager.getSubscriptionInfo();
            //             info?.PrintDebug("[PurchaseHandler] ");
            //             if (info != null)
            //             {
            //                 if(userData.SubscriptionData.UpdateSubscription(info, product.receipt))
            //                     hasReward = true;
            //                 subsUpdated = true;
            //             }
            //         }
            //
            //         if (!subsUpdated)
            //         {
            //             product = IAPManager.Instance.GetProduct(subscription.MonthlyProductId.GetProductId());
            //             if (product is { hasReceipt: true })
            //             {
            //                 var subscriptionManager = new SubscriptionManagerCustom(product, null);
            //                 var info = subscriptionManager.getSubscriptionInfo();
            //                 info?.PrintDebug("[PurchaseHandler] ");
            //                 if (info != null)
            //                 {
            //                     if(userData.SubscriptionData.UpdateSubscription(info, product.receipt))
            //                         hasReward = true;
            //                     subsUpdated = true;
            //                 }
            //             }
            //         }
            //
            //         if (!subsUpdated)
            //         {
            //             userData.SubscriptionData.DeactiveSubscription(subscription);
            //             userData.Save();
            //         }
            //     }
            //
            //     if (hasReward)
            //     {
            //         userData.Save();
            //         UIManager.Instance.ShowUIGroupOverlay<UINotifyPopup>(new UINotifyPopup.Data()
            //         {
            //             Text = "Subscription rewards has been added!",
            //         });
            //     }
            // }
        }

        public static void PurchaseItem(IapShopItem shopItem, string purchasePlace, Coupon coupon = null, Action<bool> onCompleted = null)
        {
            var productId = shopItem.GetProductId();
            if (coupon is { Type: CouponType.DiscountPrice })
            {
                var discount = shopItem.GetDiscountProductIdOfCoupon(coupon.Id);
                if (!string.IsNullOrEmpty(discount))
                {
                    productId = discount;
                }
            }

            UIManager.Instance.ShowLoading();
            MocaLib.Instance.IAPManager.PurchaseProduct(productId,
                product =>
                {
                    LogPurchaseIap(product, shopItem, purchasePlace, coupon);
                    ProcessApplyShopItem(shopItem, coupon);
                    UIManager.Instance.HideLoading();
                    onCompleted?.Invoke(true);
                },
                error =>
                {
                    Debug.LogError($"Purchase IAP failed with reason: {error}");
                    UIManager.Instance.HideLoading();
                    if (error == IapVerificationErrorId.VerifyError)
                    {
                        UIManager.Instance.ShowUIGroupOverlay<UINotify>(new UINotify.Data()
                        {
                            Text = "Purchase verification failed. Charge will be refunded within 24 hours.",
                        });
                    }

                    onCompleted?.Invoke(false);
                }, () =>
                {
                    UIManager.Instance.ShowLoading(new UILoading.Data()
                    {
                        Message = "We are processing your purchase. Just a moment...",
                    });
                });
        }

        public static void ProcessApplyPendingPurchaseShopItem(Product product, IapShopItem shopItem, Coupon coupon = null)
        {
            LogPurchaseIap(product, shopItem, "restore_pending_purchase", coupon);
            ProcessApplyShopItem(shopItem, coupon);
        }

        private static void ProcessApplyShopItem(IapShopItem shopItem, Coupon coupon = null)
        {
            var userData = UserData.Instance;

            // var bonusPercent = 0f;
            // if (coupon != null)
            // {
            //     switch (coupon.Type)
            //     {
            //         case CouponType.IncreaseReward:
            //             bonusPercent = coupon.CouponValue;
            //             break;
            //     }
            //
            //     userData.RemoveCoupon(coupon.Id);
            // }

            foreach (var item in shopItem.ItemsReward)
            {
                userData.AddItems(shopItem.GetAnalyticsBuyActionId(), item);
            }
            userData.TrackingData.AddIapShopItemBoughtTimes(shopItem.Id, 1);
            userData.TrackingData.AddDailyBoughtTimes(shopItem.Id);
            userData.Save();
        }

        public static void PurchaseSubscription(Subscription subscription, SubscriptionRenewType renewType, string purchasePlace, Action<bool> onCompleted = null)
        {
            // var productId = renewType == SubscriptionRenewType.Weekly ? subscription.WeeklyProductId.GetProductId() : subscription.MonthlyProductId.GetProductId();
            //
            // UIManager.Instance.ShowUIGroupOverlay<UILoading>();
            // IAPManager.Instance.PurchaseProduct(productId,
            //     product =>
            //     {
            //         LogPurchaseSubscription(product, subscription, renewType, purchasePlace);
            //
            //         UIManager.Instance.HideUIGroup<UILoading>();
            //
            //         var subscriptionManager = new SubscriptionManagerCustom(product, null);
            //         var info = subscriptionManager.getSubscriptionInfo();
            //         info?.PrintDebug("[PurchaseHandler] ");
            //
            //         #if !UNITY_EDITOR
            //         if (info == null || info.isSubscribed() != Result.True)
            //             return;
            //         #endif
            //
            //         IAPDataManager.OnBuySubscriptionSuccessful(subscription, product, info);
            //
            //         UIManager.Instance.ShowUIGroupOverlay<UINotifyPopup>(new UINotifyPopup.Data()
            //         {
            //             Text = "Subscription activated!",
            //         });
            //         onCompleted?.Invoke(true);
            //     },
            //     s =>
            //     {
            //         UIManager.Instance.HideUIGroup<UILoading>();
            //         onCompleted?.Invoke(false);
            //         Debug.Log($"Purchase IAP failed with reason: {s}");
            //     });
        }

        public static void ClaimCoupon(Coupon coupon)
        {
            // var userData = UserData.Instance;
            // userData.AddCoupon(coupon.Id, coupon.PlaceApply);
            // userData.TrackingData.IncreaseCouponClaimedTimes(coupon.Id, 1);
            // userData.Save();
            // switch (coupon.PlaceApply)
            // {
            //     case PlaceId.Shop:
            //         GameObject.FindAnyObjectByType<ButtonShop>()?.UpdateUI();
            //         UIManager.Instance.ShowUIGroupOverlay<UIShop>();
            //         break;
            // }
        }

        private static void LogPurchaseIap(Product product, IapShopItem shopItem, string purchasePlace, Coupon coupon)
        {
            Track.CostCenter.Iap(product.definition.id, product.metadata.localizedPrice.ToString("0.##", CultureInfo.InvariantCulture), product.metadata.isoCurrencyCode, purchasePlace);
        }

        private static void LogPurchaseSubscription(Product product, Subscription subscription, SubscriptionRenewType renewType, string purchasePlace)
        {

        }

        public enum SubscriptionRenewType
        {
            Weekly,
            Monthly
        }
    }
}