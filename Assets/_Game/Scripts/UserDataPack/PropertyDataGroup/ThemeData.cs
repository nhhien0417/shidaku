using System;
using System.Collections.Generic;
using UserDataPack.Structures;

namespace UserDataPack.PropertyDataGroup
{
    [Serializable]
    public class ThemeData : IUserDataPropertyDataGroup
    {
        public void FixData()
        {
            if (SelectedThemes == null)
            {
                SelectedThemes = new();
            }

            if (OwnedThemes == null)
            {
                OwnedThemes = new();
            }
        }

        public List<KeyValue<string>> SelectedThemes = new();
        public List<ThemeItem> OwnedThemes = new();

        public string GetSelectedThemeId(string themeType)
        {
            var selectedTheme = SelectedThemes.Find(t => t.Key == themeType);
            return selectedTheme?.Value;
        }

        public bool SelectTheme(string themeType, string themeId)
        {
            if (!OwnTheme(themeType, themeId))
            {
                return false;
            }

            var selectedTheme = SelectedThemes.Find(t => t.Key == themeType);
            if (selectedTheme != null)
            {
                selectedTheme.Value = themeId;
            }
            else
            {
                SelectedThemes.Add(new KeyValue<string>
                {
                    Key = themeType,
                    Value = themeId
                });
            }

            return true;
        }

        public bool OwnTheme(string themeType, string themeId)
        {
            return OwnedThemes.Exists(t => t.ThemeType == themeType && t.ThemeIds.Contains(themeId));
        }

        public void AddOwnedTheme(string themeType, string themeId)
        {
            var themeItem = OwnedThemes.Find(t => t.ThemeType == themeType);
            if (themeItem != null)
            {
                if (!themeItem.ThemeIds.Contains(themeId))
                {
                    themeItem.ThemeIds.Add(themeId);
                }
            }
            else
            {
                OwnedThemes.Add(new ThemeItem
                {
                    ThemeType = themeType,
                    ThemeIds = new List<string> { themeId }
                });
            }
        }
    }
}