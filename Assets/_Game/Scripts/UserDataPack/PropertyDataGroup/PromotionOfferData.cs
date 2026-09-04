using System;
using System.Collections.Generic;
using UserDataPack.Structures;

namespace UserDataPack.PropertyDataGroup
{
    [Serializable]
    public class PromotionOfferData : IUserDataPropertyDataGroup
    {
        public List<OfferData> Offers = new();

        public void FixData()
        {
            if (Offers == null)
                Offers = new();

            Offers.ForEach(x => x.FixData());
        }

        public void UpdateData()
        {
            for (var i = 0; i < Offers.Count; i++)
            {
                var offer = Offers[i];
                if (offer == null || (offer.IsActivated && offer.IsExpired()))
                {
                    Offers.RemoveAt(i);
                    i--;
                }
            }
        }

        public bool AddOffer(OfferData offer)
        {
            if (Offers.Exists(x => x.Id == offer.Id))
                return false;

            Offers.Add(offer);
            Offers.Sort((x, y) => y.Priority - x.Priority);
            return true;
        }

        public bool RemoveOffer(string offerId)
        {
            var removes = Offers.RemoveAll(x => x.Id == offerId);
            return removes > 0;
        }

        public bool RemoveOffer(OfferData offer)
        {
            return Offers.Remove(offer);
        }
    }
}