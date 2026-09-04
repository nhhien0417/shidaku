using System;
using System.Collections.Generic;
using Design;
using Design.DataHolder;

namespace UserDataPack.PropertyDataGroup
{
    [Serializable]
    public class NewVersionMigrateData : IUserDataPropertyDataGroup
    {
        public List<string> MissedBoosterIds = new();

        public void FixData()
        {
            if (MissedBoosterIds == null)
            {
                MissedBoosterIds = new();
            }
        }

        public void Migrate()
        {
            MigrateArtPuzzleSequence();
            MigrateMissedBoosters();
        }

        public void MigrateMissedBoosters()
        {
            var userData = UserData.Instance;
            var currentLevel = userData.GameplayData.CurrentGameplayLevel;
            var boosterUnlockedData = DesignDataHolder.Instance.BoosterUnlockedData;

            var missedBoosters = boosterUnlockedData.GetMissedBoosterUnlockedList(currentLevel);
            if (missedBoosters != null)
            {
                foreach (var booster in missedBoosters)
                {
                    if (!MissedBoosterIds.Contains(booster.BoosterId))
                    {
                        MissedBoosterIds.Add(booster.BoosterId);

                        var boosterUnlocked = boosterUnlockedData.GetBoosterUnlocked(booster.BoosterId);
                        userData.AddItem(boosterUnlocked.BoosterId, boosterUnlocked.RewardAmount, "unlock_missed_booster");
                        userData.FirstTimeData.MarkUnlockBoosterRewardsClaimed(boosterUnlocked.BoosterId);
                    }
                }
            }

            var claimedRewardBoosters = boosterUnlockedData.Data.FindAll(x => userData.ResourceItemDataExists(x.BoosterId));
            foreach (var booster in claimedRewardBoosters)
            {
                userData.FirstTimeData.MarkUnlockBoosterRewardsClaimed(booster.BoosterId);
            }
        }

        public List<BoosterUnlocked> GetMissedBoosters()
        {
            var boosterUnlockedData = DesignDataHolder.Instance.BoosterUnlockedData;
            var missedList = new List<BoosterUnlocked>();

            foreach (var id in MissedBoosterIds)
            {
                var info = boosterUnlockedData.GetBoosterUnlocked(id);
                if (info != null)
                {
                    missedList.Add(info);
                }
            }

            return missedList;
        }

        public void ClearMissedBoosters()
        {
            if (MissedBoosterIds.Count <= 0)
                return;

            MissedBoosterIds.Clear();
            UserData.Instance.Save();
        }

        public void MigrateArtPuzzleSequence()
        {
            UserData.Instance.ArtPuzzleData.MigrateSequenceData();
        }
    }
}
