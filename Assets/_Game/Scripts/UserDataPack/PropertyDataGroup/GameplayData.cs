using System;
using System.Runtime.Serialization;
using CodeStage.AntiCheat.ObscuredTypes;

namespace UserDataPack.PropertyDataGroup
{
    [Serializable]
    public class GameplayData : IUserDataPropertyDataGroup
    {
        public ObscuredInt CurrentLevelIndex;

        [IgnoreDataMember] public int CurrentGameplayLevel => CurrentLevelIndex + 1;

        public void FixData()
        {
        }
    }
}
