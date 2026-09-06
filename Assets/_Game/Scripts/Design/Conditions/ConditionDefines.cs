using System;

namespace Design.Conditions
{
    public abstract class ConditionId
    {
        public const string NormalModeLevelReached = "NormalModeLevelReached";
        public const string ItemAmount = "ItemAmount";
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