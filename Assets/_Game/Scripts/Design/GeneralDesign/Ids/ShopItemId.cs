using UnityEngine;

namespace Design.Ids
{
    public abstract class ShopItemId
    {
        // Normal item
        public const string FreeCoin = "free_coin";
        public const string AdsToCoin = "ads_to_coin";
        public const string AutoXLimitedTime = "autox_limited_time";
        public const string Booster1Pack = "booster_1_pack";
        public const string Booster2Pack = "booster_2_pack";
        public const string Booster3Pack = "booster_3_pack";

        // IAP item
        public const string Iap_NoAds24h = "iap_no_ads_24h";
        public const string Iap_NoAds7Days = "iap_no_ads_7_days";
        public const string Iap_NoAdsPermanent = "iap_no_ads_permanent";
        public const string Iap_CoinPack1 = "iap_coin_pack_1";
        public const string Iap_CoinPack2 = "iap_coin_pack_2";
        public const string Iap_CoinPack3 = "iap_coin_pack_3";
        public const string Iap_CoinPack4 = "iap_coin_pack_4";
        public const string Iap_UnlockAllArtPuzzles = "iap_unlock_all_art_puzzles";


        public static readonly string[] All =
        {
            "",
            FreeCoin,
            AdsToCoin,
            AutoXLimitedTime,
            Booster1Pack,
            Booster2Pack,
            Booster3Pack,
            Iap_NoAds24h,
            Iap_NoAds7Days,
            Iap_NoAdsPermanent,
            Iap_CoinPack1,
            Iap_CoinPack2,
            Iap_CoinPack3,
            Iap_CoinPack4,
            Iap_UnlockAllArtPuzzles,
        };
    }
}