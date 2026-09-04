using System;
using Common.Comparison;
using Gameplay;
using UnityEngine;
using UserDataPack;

namespace Design.Conditions.Gameplay
{
    [Serializable]
    public class AttemptNumCondition : GameplayCondition
    {
        [SerializeField] private int _attemptNum;
        [SerializeField] private ComparisonType _comparison;

        public AttemptNumCondition()
        {

        }

        public AttemptNumCondition(GameMode gameMode, int attemptNum, ComparisonType comparison) : base(gameMode)
        {
            _attemptNum = attemptNum;
            _comparison = comparison;
        }

        public override bool IsMet()
        {
            if (GameMode != GameplayManager.CurrentMode)
            {
                return false;
            }

            var levelConfigs = DesignDataHolder.Instance?.LevelConfigs;
            int windowSize = levelConfigs != null ? levelConfigs.AdjustRule.Window : 10;

            var currentPuzzleId = UserData.Instance.GameplayData.CurrentPuzzleId;
            var wonEntries = UserData.Instance.GameplayData.LevelTracking.FindAll(t =>
                (t.IsWon || t.PuzzleId == currentPuzzleId) && t.PlayMode == "normal");

            int count = Mathf.Min(wonEntries.Count, windowSize);
            if (count == 0) return false;

            var activeWindowEntries = wonEntries.GetRange(wonEntries.Count - count, count);

            float sum = 0;
            foreach (var entry in activeWindowEntries)
            {
                sum += entry.AttemptNum;
            }
            float averageAttempt = sum / count;

            return _comparison.GetResult(averageAttempt, _attemptNum);
        }
    }
}