namespace Game.PromotionOffer
{
    public abstract class PredefinedOfferId
    {
        public const string NoAdsDiscount = "NoAdsDiscount";
        public const string StarterPack = "StarterPack";
        
        public static readonly string[] All =
        {
            NoAdsDiscount,
            StarterPack,
        };
    }
}