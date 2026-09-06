using System;
using Design.Ids;
using Design.Structures;
using UnityEngine;
using UserDataPack;

public class PurchaseHandler
{
    public static bool SpendResource(string resourceId, int amount, string placement, string actionName)
    {
        switch (resourceId)
        {
            case PriceId.Free:
                return true;

            case PriceId.Ads:
                Debug.LogError("Reward Ad should be go with analytics data. Currently supporting only shop item reward ad. Please use HandleSpendResource(ShopItem shopItem) instead for now.");
                return false;
            
            case PriceId.IAP:
                Debug.LogError("IAP should be handled by Iap.PurchaseHandler. Please use PurchaseItem(IapShopItem shopItem) instead for now.");
                return false;

            default:
            {
                var userData = UserData.Instance;
                var success = userData.SpendResourceItem(resourceId, amount, actionName);
                return success;
            }
        }
    }

    public static void HandleSpendResource(Price price, string placement, string actionName, Action onSuccess = null, Action onFailure = null)
    {
        HandleSpendResource(price.Id, price.Value, placement, actionName, onSuccess, onFailure);
    }

    public static void HandleSpendResource(string resourceId, int amount, string placement, string actionName, Action onSuccess = null, Action onFailure = null)
    {
        if (SpendResource(resourceId, amount, placement, actionName))
        {
            onSuccess?.Invoke();
        }
        else
        {
            onFailure?.Invoke();
        }
    }

    private static void HandleSpendResource(ShopItem shopItem, Action onSuccess = null, Action onFailure = null) // Should not be public, use PurchaseItem instead
    {
        switch (shopItem.Price.Id)
        {
            case PriceId.Ads:
                AdsManager.ShowRewardAd(shopItem, onSuccess, onFailure);
                return;

            default:
                HandleSpendResource(shopItem.Price, shopItem.Placement, shopItem.GetAnalyticsBuyActionId(), onSuccess, onFailure);
                break;
        }
    }

    public static void PurchaseItem(ShopItem shopItem, string placement, string buttonName, Coupon coupon = null, Action<bool> onCompleted = null)
    {
        shopItem.Placement = placement;
        shopItem.ButtonName = buttonName;
        if (shopItem is IapShopItem iapShopItem)
        {
            Iap.PurchaseHandler.PurchaseItem(iapShopItem, shopItem.Placement, coupon, (success) =>
            {
                if (success)
                {
                    if (UserData.Instance.NoAdsActivated())
                    {
                        AdsManager.ShowBanner(false);
                    }
                }
                onCompleted?.Invoke(success);
            });
        }
        else
        {
            HandleSpendResource(shopItem, () =>
            {
                var userData = UserData.Instance;
                userData.ProcessPurchasedShopItem(shopItem);
                userData.Save();
                onCompleted?.Invoke(true);

                if (userData.NoAdsActivated())
                {
                    AdsManager.ShowBanner(false);
                }
            }, () =>
            {
                Debug.LogWarning("Not enough resources to purchase item!");
                ShowNotEnoughResourcesPopup(shopItem.Price.Id);

                onCompleted?.Invoke(false);
            });
        }
    }
    
    public static void PurchaseItem(ItemWithPrice item, string placement, string actionName, Action<bool> onCompleted = null)
    {
        HandleSpendResource(item.Price, placement, actionName, () =>
        {
            var userData = UserData.Instance;
            userData.AddItem(item.Id, item.Amount, $"buy_item_w_{item.Price}");
            userData.Save();
            onCompleted?.Invoke(true);

            if (userData.NoAdsActivated())
            {
                AdsManager.ShowBanner(false);
            }
        }, () =>
        {
            Debug.LogWarning("Not enough resources to purchase item!");
            ShowNotEnoughResourcesPopup(item.Price.Id);
            onCompleted?.Invoke(false);
        });
    }
    
    private static void ShowNotEnoughResourcesPopup(string resourceId)
    {
        switch (resourceId)
        {
            case ItemId.Coin:
            {
                // if (UIManager.Instance?.TopUIGroup is UIShop shop && shop.FocusToItem(ItemId.Coin))
                // {
                //     break;
                // }
                //
                // UIManager.Instance?.ShowUIGroupOverlay<UIShop>(new UIShop.Data
                // {
                //     FocusResourceId = ItemId.Coin
                // });
                break;
            }
        }
    }
}
