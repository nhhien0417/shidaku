using System;
using Design.Structures;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.PromotionOffer
{
    [Serializable]
    public class OfferDefinition
    {
        [ValueDropdown("GetAllPredefinedOfferIds")]
        public string OfferId;
        
        [SerializeReference]
        public ShopItem ShopItem;

        public Sprite PopupImage; // Use for popup
        public Sprite ShopImage; // Use in shop
        public Sprite IconImage; // Use in gameplay screen
        
        // For IAP
        public IapProductId IapProductIdBeforeDiscount;
        
        #if UNITY_EDITOR
        private string[] GetAllPredefinedOfferIds()
        {
            return PredefinedOfferId.All;
        }
        #endif
    }
}