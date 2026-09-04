using System;
using System.Collections.Generic;
using Design.Ids;
using UnityEngine.Purchasing;
using UserDataPack;

namespace Design.Structures
{
    [Serializable]
    public class IapShopItem : ShopItem
    {
        public IapProductId ProductId;
        public List<DiscountProductId> DiscountProductIds = new();
        public ProductType ProductType;

        public IapShopItem()
        {
            Price = new()
            {
                Id = PriceId.IAP,
                Value = 1,
            };
        }
        
        public string GetProductId()
        {
            return ProductId.GetProductId();
        }

        public string GetDiscountProductIdOfCoupon(string couponId)
        {
            var discountProductId = DiscountProductIds.Find(discount => discount.DiscountId == couponId);
            if (discountProductId == null)
            {
                return "";
            }

            return discountProductId.GetProductId();
        }

        public List<string> GetAllDiscountProductIds()
        {
            var discountProductIds = new List<string>();
            foreach (var discountProductId in DiscountProductIds)
            {
                discountProductIds.Add(discountProductId.GetProductId());
            }

            return discountProductIds;
        }

        public void RestorePurchase()
        {
            if (ProductType != ProductType.NonConsumable)
                return;

            var userData = UserData.Instance;
            var hasRestorableItem = false;
            foreach (var item in ItemsReward)
            {
                if (ItemId.IsInappPurchaseRestorable(item.Id))
                {
                    if (!ItemId.HasOnlyOneInstance(item.Id) || userData.GetNonconsumableItemAmount(item.Id) <= 0)
                    {
                        userData.AddItem(item.Id, item.Amount, $"restore_purchase:{GetProductId()}");
                        hasRestorableItem = true;
                    }
                }
            }
            
            if (hasRestorableItem)
                userData.Save();
        }

        public override string GetAnalyticsBuyActionId()
        {
            return $"iap:{Id}";
        }
    }
}
