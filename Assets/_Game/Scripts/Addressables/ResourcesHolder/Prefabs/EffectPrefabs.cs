using System;

namespace AssetsHolder
{
    [Serializable]
    public class EffectPrefabs : PrefabData
    {
        public const string EffectCollectItem = "Effect_CollectItem";

        public static readonly string[] All = new string[]
        {
            EffectCollectItem
        };

        #if UNITY_EDITOR
        protected override string[] GetAllIds()
        {
            return All;
        }
        #endif
    }
}
