using System;

namespace Design.Structures
{
    [Serializable]
    public class DiscountProductId
    {
        public string DiscountId;
        public IapProductId ProductId;

        public string GetProductId()
        {
            return ProductId.GetProductId();
        }
    }
}
