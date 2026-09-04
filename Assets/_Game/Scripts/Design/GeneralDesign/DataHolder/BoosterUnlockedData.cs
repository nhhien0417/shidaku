using System;
using System.Collections.Generic;
using System.Linq;
using Design.Ids;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UserDataPack;

namespace Design.DataHolder
{
    [CreateAssetMenu(fileName = "BoosterUnlockedData", menuName = "Design/BoosterUnlockedData", order = 1)]
    [Serializable]
    public class BoosterUnlockedData : ScriptableObject
    {
        public List<BoosterUnlocked> Data = new();

        public BoosterUnlocked GetBoosterUnlocked(int level)
        {
            return Data.Find(x => x.LevelUnlock == level);
        }

        public List<BoosterUnlocked> GetMissedBoosterUnlockedList(int currentLevel)
        {
            var userData = UserData.Instance;
            var data = Data.FindAll(x => currentLevel >= x.LevelUnlock && !userData.ResourceItemDataExists(x.BoosterId));
            return data;
        }

        public BoosterUnlocked GetBoosterUnlocked(string boosterId)
        {
            return Data.Find(x => x.BoosterId == boosterId);
        }

        public bool BoosterUnlockedAtLevel(string boosterId, int level, out int levelUnlocked)
        {
            levelUnlocked = 0;
            var data = Data.Find(x => x.BoosterId == boosterId);
            if (data == null)
                return true;

            levelUnlocked = data.LevelUnlock;
            return data.LevelUnlock <= level;
        }

        public void GrantRewardsForLevel(int level)
        {
            var unlock = GetBoosterUnlocked(level);
            if (unlock != null && unlock.RewardAmount > 0)
            {
                if (!UserData.Instance.FirstTimeData.HasClaimedUnlockBoosterReward(unlock.BoosterId))
                {
                    UserData.Instance.AddItem(unlock.BoosterId, unlock.RewardAmount, $"booster_unlocked");
                    UserData.Instance.FirstTimeData.MarkUnlockBoosterRewardsClaimed(unlock.BoosterId);
                }
            }
        }
    }

    [Serializable]
    public class BoosterUnlocked
    {
        private const string TutorialTableName = "Tuts";

        public int LevelUnlock;
        public string Title;
        public string Description;
        [ValueDropdown("GetAllBoosterIds")]
        public string BoosterId;
        public int RewardAmount;

        public string GetLocalizedDescription()
        {
            return GetLocalizedText(GetDescriptionKey(), Description);
        }

        public string GetRewardId()
        {
            return $"{LevelUnlock}_{BoosterId}";
        }

        private string GetDescriptionKey()
        {
            return BoosterId switch
            {
                ItemId.Booster_1 => "tut.unlock_queen",
                ItemId.Booster_2 => "tut.unlock_hint",
                ItemId.Booster_3 => "tut.unlock_random_mark",
                _ => $"tut.unlock_{BoosterId}"
            };
        }

        private static string GetLocalizedText(string key, string fallback)
        {
            if (string.IsNullOrEmpty(key))
                return fallback;

            try
            {
                var localized = LocalizationSettings.StringDatabase.GetLocalizedString(TutorialTableName, key);
                return string.IsNullOrEmpty(localized) || localized == key ? fallback : localized;
            }
            catch
            {
                return fallback;
            }
        }

#if UNITY_EDITOR
        private string[] GetAllBoosterIds()
        {
            return ItemId.AllBoosters.ToArray();
        }
#endif
    }
}
