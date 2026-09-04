using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using UserDataPack;

namespace Design.DataHolder
{
    public enum FeatureType
    {
        None,
        DailyChallenge,
        FortuneWheel,
        DayStreak,
        ArtPuzzle,
        Customize,
    }

    [Serializable]
    public class FeatureUnlockConfig
    {
        public FeatureType FeatureType;
        public int UnlockLevel;
    }

    [CreateAssetMenu(fileName = "FeatureUnlockData", menuName = "Design/FeatureUnlockData")]
    public class FeatureUnlockData : ScriptableObject
    {
        [Header("Feature Unlock Data")]
        public List<FeatureUnlockConfig> UnlockConfigs = new();

        public void ApplyRemoteConfig(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                var jsonData = JSON.Parse(json).AsArray;
                if (jsonData == null) return;

                foreach (JSONNode item in jsonData)
                {
                    foreach (var kvp in item)
                    {
                        // Check feature type
                        if (!Enum.TryParse<FeatureType>(kvp.Key, out var featureType))
                        {
                            Debug.LogWarning($"[FeatureUnlockData] Unknown feature type: {kvp.Key}");
                            continue;
                        }

                        // Check if value is a valid integer
                        if (!int.TryParse(kvp.Value.Value, out var unlockLevel))
                        {
                            Debug.LogError($"[FeatureUnlockData] Invalid unlock level for '{kvp.Key}': '{kvp.Value.Value}' is not an int");
                            continue;
                        }

                        var config = UnlockConfigs.Find(x => x.FeatureType == featureType);
                        if (config != null)
                        {
                            config.UnlockLevel = unlockLevel;
                        }
                        else
                        {
                            UnlockConfigs.Add(new FeatureUnlockConfig
                            {
                                FeatureType = featureType,
                                UnlockLevel = unlockLevel,
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FeatureUnlockData] Failed to apply remote config: {e.Message}");
            }
        }

        public int GetUnlockLevel(FeatureType featureType)
        {
            var config = UnlockConfigs.Find(x => x.FeatureType == featureType);
            return config != null ? config.UnlockLevel : 1;
        }

        public bool IsUnlocked(FeatureType featureType, int currentLevel)
        {
            var unlockLevel = GetUnlockLevel(featureType);
            if (unlockLevel < 0)
                return false;

            if (featureType == FeatureType.ArtPuzzle)
            {
                var artData = UserData.Instance.ArtPuzzleData;
                return currentLevel > unlockLevel || artData.IsFirstArtCompleted;
            }
            return currentLevel >= unlockLevel;
        }
    }
}
