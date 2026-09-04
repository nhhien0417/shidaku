using System;
using System.Collections.Generic;

namespace UserDataPack.Structures.GameplayDataGroup
{
    [Serializable]
    public class TagPlayedIds
    {
        public string Tag;
        public List<string> Ids = new();
    }

    [Serializable]
    public class LevelTrackingData
    {
        public string PlayMode;
        public string PuzzleId;
        public int AttemptNum;
        public int BoosterQueen;
        public int BoosterHint;
        public int BoosterRandomMark;
        public int CoinSpend;
        public int InterAd;
        public int RvAd;
        public float Duration;
        public bool IsWon;
    }
}
