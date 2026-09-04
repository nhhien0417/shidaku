namespace Design.Ids
{
    public abstract class CouponType
    {
        public const string DiscountPrice = "DiscountPrice";
        public const string IncreaseReward = "IncreaseReward";
        
        public static readonly string[] All =
        {
            DiscountPrice,
            IncreaseReward,
        };
    }
}