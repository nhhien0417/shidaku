using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using CodeStage.AntiCheat.ObscuredTypes;
using UserDataPack.Structures.GameplayDataGroup;

namespace UserDataPack.PropertyDataGroup
{
    [Serializable]
    public class GameplayData : IUserDataPropertyDataGroup
    {
        public ObscuredInt CurrentLevelIndex;
        public ObscuredInt DifficultOffset; //OBSOLETE. DO NOT USE. Use UserDifficulty instead
        public ObscuredFloat UserDifficulty;
        public string CurrentPuzzleId;
        public bool TutorialCompleted;
        public bool PrevLevelIsArt;

        public List<string> AutoXFreePuzzleIds = new();
        public List<TagPlayedIds> PlayedIdsPerTag = new();
        public List<LevelTrackingData> LevelTracking = new();

        [IgnoreDataMember] public int CurrentGameplayLevel => CurrentLevelIndex + 1;

        public void FixData()
        {
            PlayedIdsPerTag ??= new();
            LevelTracking ??= new();
            AutoXFreePuzzleIds ??= new();
        }
    }
}