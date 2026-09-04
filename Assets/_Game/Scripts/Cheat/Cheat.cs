using _Game.Scripts.Cheat;
using RemoteConfigs;
using Titipi.MocaLib.Runtime.Services;
using UnityEngine;
using UnityEngine.Events;

public class Cheat : MonoBehaviour
{
    [SerializeField] private UnityEvent onActiveSuccessful;

    public void ActivateSRDebugger()
    {
        Debug.Log("Trying to activate SRDebugger...");
        
        var remoteConfig = RemoteConfigHelper.Instance;
        if (remoteConfig == null)
            return;

        if (!remoteConfig.GetConfig(RemoteConfigKey.CHEATS_ENABLED, false)) return;

        if (SRDebug.Instance == null || !SRDebug.IsInitialized)
        {
            SRDebug.Init();
        }

        SRDebug.Instance.ShowDebugPanel();

        if (DataHelper.ShowDebugUI)
        {
            FindAnyObjectByType<DebugUI>(FindObjectsInactive.Include)?.gameObject.SetActive(true);
        }

        onActiveSuccessful?.Invoke();
    }
}