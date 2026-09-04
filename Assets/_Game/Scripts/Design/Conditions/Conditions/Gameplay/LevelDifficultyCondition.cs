using System;
using Common.Comparison;
using Gameplay;
using UnityEngine;
using UserDataPack;

namespace Design.Conditions.Gameplay
{
    [Serializable]
    public class LevelDifficultyCondition : GameplayCondition
    {
        [SerializeField] private float _value;
        [SerializeField] private ComparisonType _comparison;

        public LevelDifficultyCondition()
        {

        }

        public LevelDifficultyCondition(GameMode gameMode, float value, ComparisonType comparison) : base(gameMode)
        {
            _value = value;
            _comparison = comparison;
        }

        public override bool IsMet()
        {
            if (GameMode != GameplayManager.CurrentMode)
            {
                return false;
            }

            int currentLevel = UserData.Instance.GameplayData.CurrentGameplayLevel;
            var levelConfigs = DesignDataHolder.Instance != null ? DesignDataHolder.Instance.LevelConfigs : null;
            float difficulty = levelConfigs != null ? levelConfigs.GetBaseLevelDifficulty(currentLevel) : 0f;
            float userDiff = GameMode == GameMode.Normal ? UserData.Instance.GameplayData.UserDifficulty : 0f;

            return _comparison.GetResult(difficulty + userDiff, _value);
        }
    }
}
