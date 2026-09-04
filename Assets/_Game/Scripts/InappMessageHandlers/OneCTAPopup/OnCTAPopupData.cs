using System.Collections.Generic;
using Design.Structures;

namespace Game.InappMessageHandlers.OnCTAPopupData
{
    public class FreeGiftData
    {
        public List<Item> Items = new ();
        public string PopupImageUrl;

        public static FreeGiftData Parse(Dictionary<string, string> data)
        {
            // Offer items
            data.TryGetValue(InAppMessageDataKey.Items, out var itemsStr);
            var items = itemsStr.ParseToItems();
            if (items is {Count: <= 0})
                return null;

            // Image url
            data.TryGetValue(InAppMessageDataKey.PopupImageUrl, out var imageUrl);

            return new FreeGiftData()
            {
                Items = items,
                PopupImageUrl = imageUrl,
            };
        }
    }
}