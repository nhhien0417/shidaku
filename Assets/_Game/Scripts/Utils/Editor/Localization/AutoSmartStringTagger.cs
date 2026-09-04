using UnityEditor;
using UnityEngine;

public static class AutoSmartStringTagger
{
    [MenuItem("Tools/Localization/Auto Tag Smart Strings")]
    public static void AutoTag()
    {
        int updatedCount = AutoSmartStringUtility.ApplyToAllCollections();
        Debug.Log($"[Localization] Auto Smart String updated {updatedCount} entries.");
    }
}