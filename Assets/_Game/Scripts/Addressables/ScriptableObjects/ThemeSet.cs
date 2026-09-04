using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Design.Ids;
using UnityEngine;

namespace AssetsHolder
{
    [CreateAssetMenu(fileName = "ThemeSet", menuName = "Design/ThemeSet", order = 1)]
    [Serializable]
    public class ThemeSet : ScriptableObject
    {
        [ValueDropdown("GetAllThemeIds")]
        public string Id;
        [SerializeField] protected List<Data<Sprite>> Sprites = new();
        [SerializeField] protected List<Data<Color>> Colors = new();

        public Sprite GetSprite(string themeType)
        {
            var spriteItem = Sprites.Find(s => s.Key == themeType);
            return spriteItem != null ? spriteItem.Value : null;
        }

        public Color GetColor(string themeType)
        {
            var colorItem = Colors.Find(c => c.Key == themeType);
            return colorItem != null ? colorItem.Value : Color.white;
        }
        
#if UNITY_EDITOR
        protected virtual string[] GetAllThemeIds()
        {
            return ThemeId.All;
        }
#endif

        [Serializable]
        protected class Data<T>
        {
            [ValueDropdown("GetAllThemeTypes")]
            public string Key;
            public T Value;
            
        #if UNITY_EDITOR
            private string[] GetAllThemeTypes() => ThemeType.All;
        #endif
        }
    }
}