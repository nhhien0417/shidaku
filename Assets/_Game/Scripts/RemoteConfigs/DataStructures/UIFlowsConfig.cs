using System;
using SimpleJSON;
using Sirenix.OdinInspector;
using UI.Common;
using UnityEngine;

namespace RemoteConfigs
{
    [Serializable]
    public class UIFlowsConfig
    {
        [ValueDropdown("GetAllWinFlows")]
        public int WinFlow = UIFlowId.WinFlow.Default;

        public void ApplyRemoteConfig(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                var jsonData = JSON.Parse(json);
                if (jsonData == null) return;

                if (jsonData["win_flow"] != null)
                {
                    WinFlow = jsonData["win_flow"].AsInt;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIFlowsConfig] Failed to apply remote config: {e.Message}");
            }
        }

#if UNITY_EDITOR
        private ValueDropdownList<int> GetAllWinFlows()
        {
            return UIFlowId.WinFlow.GetDropdownOptions();
        }
#endif
    }
}