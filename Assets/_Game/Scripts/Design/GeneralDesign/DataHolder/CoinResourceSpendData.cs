using System;
using System.Collections.Generic;
using System.Linq;
using Design.Ids;
using SimpleJSON;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Design.DataHolder
{
    [CreateAssetMenu(fileName = "CoinResourceSpendData", menuName = "Design/CoinResourceSpendData")]
    [Serializable]
    public class CoinResourceSpendData : ScriptableObject
    {
        [Header("Continue On Lose")]
        public int ContinueCost;

        [Header("Restore Day Streak")]
        public int StreakRestoreFirstDayCost;
        public int StreakRestoreExtraDayCost;

        [Header("Unlock Daily Challenge Day")]
        public int DailyChallengeUnlockCost;

        [Header("Buy Booster In Gameplay")]
        public int DefaultBoosterBuyAmount;
        public int DefaultBoosterBuyPrice;
        public List<BoosterPriceEntry> BoosterPrices = new();

        public int GetStreakRestoreCost(int missedDays)
        {
            if (missedDays <= 0)
                return 0;

            return StreakRestoreFirstDayCost + Math.Max(0, missedDays - 1) * StreakRestoreExtraDayCost;
        }

        public (int amount, int price) GetBoosterBuyOption(string boosterId)
        {
            var entry = BoosterPrices?.Find(e => e.BoosterId == boosterId);
            if (entry != null)
                return (entry.Amount, entry.Price);

            return (DefaultBoosterBuyAmount, DefaultBoosterBuyPrice);
        }

        public void ApplyRemoteConfig(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                var root = JSON.Parse(json);
                if (root == null)
                    return;

                if (root["continue_cost"] != null)
                    ContinueCost = root["continue_cost"].AsInt;

                if (root["streak_restore_first_day_cost"] != null)
                    StreakRestoreFirstDayCost = root["streak_restore_first_day_cost"].AsInt;

                if (root["streak_restore_extra_day_cost"] != null)
                    StreakRestoreExtraDayCost = root["streak_restore_extra_day_cost"].AsInt;

                if (root["daily_challenge_unlock_cost"] != null)
                    DailyChallengeUnlockCost = root["daily_challenge_unlock_cost"].AsInt;

                if (root["default_booster_buy_amount"] != null)
                    DefaultBoosterBuyAmount = root["default_booster_buy_amount"].AsInt;

                if (root["default_booster_buy_price"] != null)
                    DefaultBoosterBuyPrice = root["default_booster_buy_price"].AsInt;

                var boosterPricesNode = root["booster_prices"];
                if (boosterPricesNode != null && boosterPricesNode.IsArray)
                {
                    var entries = new List<BoosterPriceEntry>();
                    foreach (JSONNode entryNode in boosterPricesNode.AsArray)
                    {
                        var boosterId = entryNode["booster_id"]?.Value;
                        if (string.IsNullOrEmpty(boosterId))
                            continue;

                        var amount = entryNode["amount"] != null ? entryNode["amount"].AsInt : 0;
                        var price = entryNode["price"] != null ? entryNode["price"].AsInt : 0;
                        if (amount <= 0)
                            continue;

                        entries.Add(new BoosterPriceEntry { BoosterId = boosterId, Amount = amount, Price = price });
                    }

                    if (entries.Count > 0)
                        BoosterPrices = entries;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CoinResourceSpendData] Failed to apply remote config: {e.Message}");
            }
        }
    }

    [Serializable]
    public class BoosterPriceEntry
    {
        [ValueDropdown("GetAllBoosterIds")]
        public string BoosterId;
        public int Amount;
        public int Price;

#if UNITY_EDITOR
        private string[] GetAllBoosterIds()
        {
            return ItemId.AllBoosters.ToArray();
        }
#endif
    }
}
