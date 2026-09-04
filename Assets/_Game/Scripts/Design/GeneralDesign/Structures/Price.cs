using System;
using Design.Ids;
using Sirenix.OdinInspector;

namespace Design.Structures
{
    [Serializable]
    public struct Price
    {
        [ValueDropdown("GetAllPriceIds")]
        public string Id;

        public int Value;

        public string GetPriceAsString()
        {
            switch (Id)
            {
                case PriceId.Ads:
                case PriceId.Free:
                    return "Free";
                default:
                    return Value.ToResourceValueString();
            }
        }

#if UNITY_EDITOR
        private string[] GetAllPriceIds()
        {
            return PriceId.All;
        }
#endif
    }
}