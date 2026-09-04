using System;
using System.Collections.Generic;

namespace Design.Ids
{
    public abstract class ThemeId // IMPORTANT: Remember to update All when add new id
    {
        public const string Default = "theme_default";
        public const string Summer = "theme_summer";
        public const string Halloween = "theme_halloween";
        public const string Christmas = "theme_christmas";
        
        public static readonly string[] All =
        {
            Default,
            Summer,
            Halloween,
            Christmas,
        };
    }

    public abstract class ThemeType
    {
        public const string Board = "board";
        public const string Queen = "queen";
    
        public static readonly string[] All =
        {
            Board,
            Queen,
        };
    }
}