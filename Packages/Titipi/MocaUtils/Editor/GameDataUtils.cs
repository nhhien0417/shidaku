#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Titipi.Editor
{
    public class GameDataUtils
    {
        [MenuItem("Tools/Game Data/Open persistentDataPath folder")]
        private static void OpenPersistentDataPath()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        [MenuItem("Tools/Game Data/Clear all PlayerPrefs")]
        private static void ClearAllPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}

#endif
