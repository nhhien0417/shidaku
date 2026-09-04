using System;
using System.Linq;
using Design.Ids;
using Sirenix.OdinInspector;
using UnityEngine;
using UserDataPack;

namespace Design.Conditions
{
    [Serializable]
    public class BoosterUnlockedCondition : Condition
    {
        [SerializeField][ValueDropdown("GetAllBoosterIds")] protected string _boosterId;

        public BoosterUnlockedCondition()
        {

        }

        public BoosterUnlockedCondition(string boosterId)
        {
            _boosterId = boosterId;
        }

        public override bool IsMet()
        {
            var userData = UserData.Instance;
            var boosterUnlocked = DesignDataHolder.Instance?.BoosterUnlockedData?.BoosterUnlockedAtLevel(_boosterId, userData.GameplayData.CurrentGameplayLevel, out _);
            return boosterUnlocked ?? true;
        }

#if UNITY_EDITOR
        private string[] GetAllBoosterIds()
        {
            return ItemId.AllBoosters.ToArray();
        }
#endif
    }
}