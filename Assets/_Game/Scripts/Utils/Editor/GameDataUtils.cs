#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public class GameDataUtils
{
    [MenuItem("Tools/Game Data/Clear game data")]
    private static void ClearGameData()
    {
        File.Delete(Path.Combine(Application.persistentDataPath, "usrobs_v2.bin"));
    }
}
#endif