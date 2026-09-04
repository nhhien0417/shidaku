using Design.Structures;
using UserDataPack;

namespace Design.Utils
{
    public static class ShopItemUtils
    {
        public static bool IsShopItemAvailable(ShopItem shopItem)
        {
            if (shopItem == null || !shopItem.IsUnlocked())
                return false;
        
            var userData = UserData.Instance;

            if (shopItem.HasPurchaseLimit)
            {
                var boughtTimes = userData.TrackingData.GetShopItemBoughtTimes(shopItem);
                return boughtTimes < shopItem.PurchaseLimit;
            }
            else if (shopItem.HasDailyPurchaseLimit)
            {
                var boughtTimes = userData.TrackingData.GetDailyBoughtTimes(shopItem.Id);
                return boughtTimes < shopItem.DailyPurchaseLimit;
            }

            return true;
        }
    }
}