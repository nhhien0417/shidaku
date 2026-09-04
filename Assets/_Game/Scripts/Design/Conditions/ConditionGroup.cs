using System.Collections.Generic;

namespace Design.Conditions
{
    public class ConditionGroup
    {
        public ConditionMatchType MatchType;
        public List<Condition> Conditions;

        /// <summary>
        /// Return true if all conditions are meant to be met
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            return Conditions is not null && Conditions.Count > 0;
        }

        /// <summary>
        /// Return true if all/any conditions are met based on MatchType
        /// </summary>
        /// <returns></returns>
        public bool IsMet()
        {
            if (Conditions is null || Conditions.Count == 0)
                return true;

            switch (MatchType)
            {
                case ConditionMatchType.All:
                    return Conditions.TrueForAll(x => x.IsMet());

                case ConditionMatchType.Any:
                    return Conditions.Exists(x => x.IsMet());

                default:
                    return false;
            }
        }

        public bool IsValidAndMet()
        {
            return IsValid() && IsMet();
        }
    }
}