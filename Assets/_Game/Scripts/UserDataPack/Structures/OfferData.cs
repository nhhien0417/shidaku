using System;
using Design.Conditions;

namespace UserDataPack.Structures
{
    [Serializable]
    public class OfferData
    {
        public string Id;
        public int Priority;
        public bool IsActivated;
        public int UnlockLevel;
        public string ActiveConditions;
        public string RemoveConditions;
        public string CustomData;
        public LocalLimitedTimeData LocalLimitedTimeData;

        public int LimitedTimeInSeconds;

        [Obsolete("Use LocalLimitedTimeData instead")]
        public LimitedTimeData LimitedTimeData;

        public OfferData(string id, int priority, int limitedTimeInSeconds, int unlockLevel, string activeConditions, string removeConditions, string customData)
        {
            Id = id;
            Priority = priority;
            IsActivated = false;
            LimitedTimeInSeconds = limitedTimeInSeconds;
            UnlockLevel = unlockLevel;
            ActiveConditions = activeConditions;
            RemoveConditions = removeConditions;
            CustomData = customData;
        }

        public void FixData()
        {
#pragma warning disable CS0618 // Type or member is obsolete - Migrate from LimitedTimeData to LocalLimitedTimeData
            if (LimitedTimeData != null)
            {
                LocalLimitedTimeData = new LocalLimitedTimeData()
                {
                    ExpiredDate = LimitedTimeData.ExpiredDate
                };
                LimitedTimeData = null;
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public bool IsExpired()
        {
            return LocalLimitedTimeData != null && LocalLimitedTimeData.IsExpired();
        }

        public bool ActiveOffer()
        {
            if (IsActivated)
                return false;

            if (UnlockLevel > 0)
            {
                var currentLevel = UserData.Instance.GameplayData.CurrentGameplayLevel;
                if (currentLevel < UnlockLevel)
                    return false;
            }

            if (IsNeedToBeRemoved())
                return false;

            if (!string.IsNullOrEmpty(ActiveConditions) && !ActiveConditions.ConditionsIsValidAndMet())
                return false;

            if (LimitedTimeInSeconds > 0)
            {
                LocalLimitedTimeData = new();
                LocalLimitedTimeData.AddLimitedTime(LimitedTimeInSeconds);
            }

            IsActivated = true;
            return true;
        }

        public bool IsNeedToBeRemoved()
        {
            return !string.IsNullOrEmpty(RemoveConditions) && RemoveConditions.ConditionsIsValidAndMet();
        }
    }
}