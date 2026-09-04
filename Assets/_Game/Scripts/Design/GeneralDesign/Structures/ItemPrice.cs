using System;
using Design.Ids;
using Sirenix.OdinInspector;

namespace Design.Structures
{
    [Serializable]
    public class ItemPrice
    {
        [ValueDropdown("GetAllItemIds")]
        public string Id;

        public Price Price;

#if UNITY_EDITOR
        private string[] GetAllItemIds()
        {
            return ItemId.All;
        }
#endif
    }
}