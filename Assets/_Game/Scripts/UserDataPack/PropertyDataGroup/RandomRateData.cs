using System;
using System.Collections.Generic;

namespace UserDataPack.PropertyDataGroup
{
    [Serializable]
    public class RandomRateData : IUserDataPropertyDataGroup
    {
        public float RandomMarkDropRateNormalMode = 0.05f; //OBSOLETE. DO NOT USE. Use BoosterPityRates instead

        public Dictionary<string, float> BoosterPityRates = new();

        public void FixData()
        {
            BoosterPityRates ??= new();
        }

        public float GetPityRate(string boosterId, float defaultRate)
        {
            return BoosterPityRates.TryGetValue(boosterId, out var rate) ? rate : defaultRate;
        }

        public void SetPityRate(string boosterId, float rate)
        {
            BoosterPityRates[boosterId] = rate;
        }
    }
}
