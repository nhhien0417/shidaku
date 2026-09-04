using System;

namespace Design.Conditions
{
    public abstract class ConditionId
    {
        public const string NormalModeLevelReached = "NormalModeLevelReached";
        public const string BoosterUnlocked = "BoosterUnlocked";
        public const string ItemAmount = "ItemAmount";
        public const string Gameplay_Normal_TotalPuzzleSolveTime = "G_Normal_TotalPuzzleSolveTime";
        public const string Gameplay_Normal_BoostersUsed = "G_Normal_BoostersUsed";
        public const string Gameplay_Normal_AttemptNum = "G_Normal_AttemptNum";
        public const string Gameplay_Normal_LevelDifficulty = "G_Normal_LevelDifficulty";
    }

    public enum ConditionMatchType
    {
        All,
        Any,
    }

    public static class ConditionDefinesExtension
    {
        public static bool TryToConditionMatchType(this string str, out ConditionMatchType matchType)
        {
            if (string.Equals(str, "all", StringComparison.OrdinalIgnoreCase))
            {
                matchType = ConditionMatchType.All;
                return true;
            }

            if (string.Equals(str, "any", StringComparison.OrdinalIgnoreCase))
            {
                matchType = ConditionMatchType.Any;
                return true;
            }

            matchType = default;
            return false;
        }
    }
}