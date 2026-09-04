using System;
using System.Collections.Generic;
using UnityEngine;
using Design.Structures;
using Game.PromotionOffer;

namespace Design.DataHolder
{
    [CreateAssetMenu(fileName = "PredefinedOfferData", menuName = "Design/PredefinedOfferData")]
    [Serializable]
    public class PredefinedOfferData : ScriptableObject
    {
        [SerializeField] public List<OfferDefinition> _offers = new ();

        public List<OfferDefinition> Offers => _offers;
        
        public OfferDefinition Get(string offerId)
        {
            return _offers?.Find(x => x.OfferId == offerId);
        }
    }
}