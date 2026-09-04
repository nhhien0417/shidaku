using System;
using UnityEngine;

namespace Design.Conditions
{
    [Serializable]
    public class NormalModeLevelReachedCondition : Condition
    {
        [SerializeField] protected int _requiredLevel;

        public NormalModeLevelReachedCondition()
        {

        }

        public NormalModeLevelReachedCondition(int requiredLevel)
        {
            _requiredLevel = requiredLevel;
        }

        public override bool IsMet()
        {
            var userData = UserDataPack.UserData.Instance;
            return userData.GameplayData.CurrentGameplayLevel >= _requiredLevel;
        }
    }
}