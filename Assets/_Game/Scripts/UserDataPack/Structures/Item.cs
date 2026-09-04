using System;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;

namespace UserDataPack.Structures
{
    [Serializable]
    public class Item
    {
        public string Id;
        public ObscuredInt Amount;
    }

    [Serializable]
    public class ThemeItem
    {
        public string ThemeType; // What it applies to. Can be found in ThemeType
        public List<string> ThemeIds; // This is what id user owned. Can be found in ThemeId
    }

    [Serializable]
    public class ArtPuzzleItem
    {
        public string ArtPuzzleId; // Big picture
        public List<int> PieceIds; // Pieces owned for that picture. Ex: Picture has 20 pieces, if user owned piece 1, 5, 7, PieceIds = [1,5,7]
    }
}
