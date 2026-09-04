using System.Collections.Generic;
using System.Linq;
using Design.DataHolder;
using Design.Ids;
using Design.Structures;
using UnityEngine;
using Design.Utils;
using Game.PromotionOffer;
using Titipi.MocaLib.Runtime.Services;
using UnityEngine.Purchasing;
using UserDataPack;
using RemoteConfigs;

namespace Design
{
    public class DesignDataHolder : SingletonComponent<DesignDataHolder>
    {
        public IapShopItemData IapShopItemData;
        public ShopItemData NormalShopItemData;
        public CustomizeShopItemData CustomizeShopItemData;
        // public SubscriptionData SubscriptionData;
        public LevelSettingsData LevelSettingsData;
        public LevelLabelData LevelLabelData;
        public LevelConfigs LevelConfigs;

        public BoosterUnlockedData BoosterUnlockedData;
        public FeatureUnlockData FeatureUnlockData;

        public FixedLevelSequenceData FixedLevelSequenceData;
        public ArtLevelSequenceData ArtLevelSequenceData;

        public ItemConversionData ItemConversionData;
        public RestorableItems RestorableItems;
        public ItemPriceData ItemPriceData;
        public RandomItemData RandomItemData;

        public DailyChallengeRewardsData DailyChallengeRewardsData;
        public DayStreakRewardData DayStreakRewardData;
        public FortuneWheelRewardData FortuneWheelRewardData;
        public LevelRewardData LevelRewardData;
        public CoinResourceSpendData CoinResourceSpendData;

        public PredefinedOfferData PredefinedOfferData;

        public UIFlowsConfig UIFlowsConfig = new();
        public PlaytimeGroupConfig PlaytimeGroupConfig = new();
        public LevelGroupConfig LevelGroupConfig = new();

        public void ApplyRemoteConfig()
        {
            var remoteConfigMng = RemoteConfigHelper.Instance;
            if (remoteConfigMng == null)
                return;

            var fortuneWheelReward = remoteConfigMng.GetConfig(RemoteConfigKey.FORTUNE_WHEEL_CONFIG, "");
            if (!string.IsNullOrEmpty(fortuneWheelReward))
            {
                FortuneWheelRewardData.ApplyRemoteConfig(fortuneWheelReward);
            }

            var featureUnlock = remoteConfigMng.GetConfig(RemoteConfigKey.FEATURE_UNLOCK_CONFIG, "");
            if (!string.IsNullOrEmpty(featureUnlock))
            {
                FeatureUnlockData.ApplyRemoteConfig(featureUnlock);
            }

            var levelConfig = remoteConfigMng.GetConfig(RemoteConfigKey.LEVEL_CONFIGS, "");
            LevelConfigs.ApplyRemoteConfig(levelConfig);

            var levelLabelConfig = remoteConfigMng.GetConfig(RemoteConfigKey.LEVEL_LABEL_CONFIG, "");
            if (!string.IsNullOrEmpty(levelLabelConfig))
            {
                LevelLabelData.ApplyRemoteConfig(levelLabelConfig);
            }

            var uiFlows = remoteConfigMng.GetConfig(RemoteConfigKey.UI_FLOWS, "");
            if (!string.IsNullOrEmpty(uiFlows))
            {
                UIFlowsConfig.ApplyRemoteConfig(uiFlows);
            }

            var playtimeGroups = remoteConfigMng.GetConfig(RemoteConfigKey.PLAYTIME_GROUPS_CONFIG, "");
            if (!string.IsNullOrEmpty(playtimeGroups))
            {
                PlaytimeGroupConfig.ApplyRemoteConfig(playtimeGroups);
            }

            var levelGroups = remoteConfigMng.GetConfig(RemoteConfigKey.LEVEL_GROUPS_CONFIG, "");
            if (!string.IsNullOrEmpty(levelGroups))
            {
                LevelGroupConfig.ApplyRemoteConfig(levelGroups);
            }

            var shopItemConfig = remoteConfigMng.GetConfig(RemoteConfigKey.SHOP_CONFIG, "");
            if (!string.IsNullOrEmpty(shopItemConfig))
            {
                NormalShopItemData.ApplyRemoteConfig(shopItemConfig);
                Iap.IapCatalogSync.SyncNewProducts(GetAllIapProductIds());
            }

            var levelRewardConfig = remoteConfigMng.GetConfig(RemoteConfigKey.LEVEL_REWARD_CONFIG, "");
            if (!string.IsNullOrEmpty(levelRewardConfig))
            {
                LevelRewardData.ApplyRemoteConfig(levelRewardConfig);
            }

            var coinResourceSpendConfig = remoteConfigMng.GetConfig(RemoteConfigKey.COIN_RESOURCE_SPEND_CONFIG, "");
            if (!string.IsNullOrEmpty(coinResourceSpendConfig))
            {
                CoinResourceSpendData.ApplyRemoteConfig(coinResourceSpendConfig);
            }
        }

        public bool HasFreeCoinRewardInShop(out ShopItem shopItem)
        {
            if (!NormalShopItemData.IsGroupUnlocked(ShopItemId.FreeCoin))
            {
                shopItem = null;
                return false;
            }

            shopItem = NormalShopItemData.GetShopItem(ShopItemId.FreeCoin);
            if (shopItem != null)
            {
                if (shopItem.HasNoPurchaseLimit)
                {
                    Debug.LogError($"ShopItem {shopItem.Id} does not have purchase limit or daily purchase limit, which is unexpected for free coin item");
                    shopItem = null;
                }
                else if (!ShopItemUtils.IsShopItemAvailable(shopItem))
                {
                    shopItem = null;
                }
            }

            if (shopItem == null)
            {
                shopItem = NormalShopItemData.GetShopItem(ShopItemId.AdsToCoin);
                if (shopItem != null && !ShopItemUtils.IsShopItemAvailable(shopItem))
                {
                    shopItem = null;
                }
            }

            return shopItem != null;
        }

        public List<(string productId, ProductType productType)> GetAllIapProductIds()
        {
            var result = new List<(string productId, ProductType productType)>();

            var iapShopItems = IapShopItemData?.GetAllItems();
            if (iapShopItems != null)
            {
                foreach (var item in iapShopItems)
                {
                    result.Add((item.GetProductId(), item.ProductType));

                    var discountIds = item.GetAllDiscountProductIds();
                    foreach (var discountId in discountIds)
                    {
                        result.Add((discountId, item.ProductType));
                    }
                }
            }

            var offerItems = PredefinedOfferData?.Offers;
            if (offerItems != null)
            {
                foreach (var offer in offerItems)
                {
                    var item = offer.ShopItem;
                    if (item is IapShopItem iapItem)
                    {
                        result.Add((iapItem.GetProductId(), iapItem.ProductType));

                        var discountIds = iapItem.GetAllDiscountProductIds();
                        foreach (var discountId in discountIds)
                        {
                            result.Add((discountId, iapItem.ProductType));
                        }
                    }
                }
            }

            if (CustomizeShopItemData != null)
            {
                var customizeItems = CustomizeShopItemData.GetAllCustomizeItems();
                if (customizeItems != null)
                {
                    foreach (var item in customizeItems)
                    {
                        if (item is IapShopItem iapItem)
                        {
                            result.Add((iapItem.GetProductId(), iapItem.ProductType));

                            var discountIds = iapItem.GetAllDiscountProductIds();
                            foreach (var discountId in discountIds)
                            {
                                result.Add((discountId, iapItem.ProductType));
                            }
                        }
                    }
                }
            }

            return result;
        }

        public void RestorePurchasedNonConsumableItems(List<Product> purchasedProducts)
        {
            if (purchasedProducts is { Count: <= 0 })
                return;

            var handledProducts = new List<Product>();

            // Restore in IapShopItems
            var iapShopItems = IapShopItemData?.Items;
            if (iapShopItems != null)
            {
                foreach (var product in purchasedProducts)
                {
                    if (product.hasReceipt && product.definition.type == ProductType.NonConsumable)
                    {
                        var productId = product.definition.id;
                        var shopItem = iapShopItems.Find(x => x.ProductId.GetProductId() == productId || x.DiscountProductIds.Exists(d => d.GetProductId() == productId));
                        if (shopItem != null)
                        {
                            shopItem.RestorePurchase();
                            handledProducts.Add(product);
                        }
                    }
                }
            }

            // Remove handled products
            foreach (var product in handledProducts)
            {
                purchasedProducts.Remove(product);
            }
            handledProducts.Clear();

            // Restore in Predefined Offers
            var predefinedOffers = PredefinedOfferData?.Offers;
            if (predefinedOffers != null)
            {
                foreach (var product in purchasedProducts)
                {
                    if (product.hasReceipt && product.definition.type == ProductType.NonConsumable)
                    {
                        var productId = product.definition.id;
                        var offer = predefinedOffers.Find(x => x.ShopItem is IapShopItem iapItem
                                                                && (iapItem.ProductId.GetProductId() == productId ||
                                                                    iapItem.DiscountProductIds.Exists(d =>
                                                                        d.GetProductId() == productId)));
                        if (offer is { ShopItem: IapShopItem iapShopItem })
                        {
                            iapShopItem.RestorePurchase();
                            handledProducts.Add(product);
                        }
                    }
                }
            }

            // Remove handled products
            foreach (var product in handledProducts)
            {
                purchasedProducts.Remove(product);
            }
            handledProducts.Clear();

            // Restore in CustomizeShopItems
            if (CustomizeShopItemData != null)
            {
                var customizeItems = CustomizeShopItemData.GetAllCustomizeItems();
                if (customizeItems != null)
                {
                    foreach (var product in purchasedProducts)
                    {
                        if (product.hasReceipt && product.definition.type == ProductType.NonConsumable)
                        {
                            var productId = product.definition.id;
                            var shopItem = customizeItems.Find(x => x is IapShopItem iapItem
                                                                    && (iapItem.ProductId.GetProductId() == productId ||
                                                                        iapItem.DiscountProductIds.Exists(d =>
                                                                            d.GetProductId() == productId)));
                            if (shopItem is IapShopItem iapShopItem)
                            {
                                iapShopItem.RestorePurchase();
                                handledProducts.Add(product);
                            }
                        }
                    }

                    // Remove handled products
                    foreach (var product in handledProducts)
                    {
                        purchasedProducts.Remove(product);
                    }
                    handledProducts.Clear();
                }
            }
        }

        public List<Item> ProcessPendingPurchase(Product product)
        {
            if (product is not { hasReceipt: true })
                return new();

            var productId = product.definition.id;
            IapShopItem shopItem = null;

            var offer = PredefinedOfferData?.Offers.Find(x => x.ShopItem is IapShopItem iapItem
                                                             && (iapItem.ProductId.GetProductId() == productId ||
                                                                 iapItem.DiscountProductIds.Exists(d =>
                                                                     d.GetProductId() == productId)));
            if (offer != null)
            {
                if (!PromotionOfferManager.Instance.RemoveActiveOffer(offer.OfferId, true))
                {
                    UserData.Instance.PromotionOfferData.RemoveOffer(offer.OfferId);
                    UserData.Instance.Save();
                }
                shopItem = offer.ShopItem as IapShopItem;
            }

            if (shopItem == null)
            {
                shopItem = IapShopItemData?.GetAllItems().FirstOrDefault(x => x.ProductId.GetProductId() == productId || x.DiscountProductIds.Exists(d => d.GetProductId() == productId));
            }

            if (shopItem == null && CustomizeShopItemData != null)
            {
                var customizeItems = CustomizeShopItemData.GetAllCustomizeItems();
                if (customizeItems != null)
                {
                    var item = customizeItems.Find(x => x is IapShopItem iapItem
                                                        && (iapItem.ProductId.GetProductId() == productId ||
                                                            iapItem.DiscountProductIds.Exists(d =>
                                                                d.GetProductId() == productId)));
                    if (item is IapShopItem iapShopItem)
                        shopItem = iapShopItem;
                }
            }

            if (shopItem != null)
            {
                Iap.PurchaseHandler.ProcessApplyPendingPurchaseShopItem(product, shopItem);
                return shopItem.ItemsReward;
            }

            return new();
        }
    }
}
