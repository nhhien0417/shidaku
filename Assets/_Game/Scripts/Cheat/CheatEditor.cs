using _Game.Scripts.Cheat;
using UnityEngine;

public class CheatEditor : MonoBehaviour
{
#if UNITY_EDITOR
    private void Awake()
    {
        if (SRDebug.Instance == null || !SRDebug.IsInitialized)
        {
            SRDebug.Init();
        }

        if (DataHelper.ShowDebugUI)
        {
            FindAnyObjectByType<DebugUI>(FindObjectsInactive.Include)?.gameObject.SetActive(true);
        }
    }
#endif
}
