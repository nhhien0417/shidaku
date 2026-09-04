using UnityEngine;
using CostCenter.Attribution;
using CostCenter.RemoteConfig;

namespace CostCenter {
    public class CCServices : MonoBehaviour
    {
        [SerializeField] private bool _dontDestroy = false;

        void Start() {
            if (_dontDestroy) {
                DontDestroyOnLoad(gameObject);
            }
        }

        public void AddAttribution()
        {
            if (gameObject.GetComponent<CCAttribution>()) {
                Debug.LogError("CCAttribution has already added.");
                return;
            }
            gameObject.AddComponent<CCAttribution>();
        }

        public void AddRemoteConfig()
        {
            if (gameObject.GetComponent<CCRemoteConfig>()) {
                Debug.LogError("CCRemoteConfig has already added.");
                return;
            }
            gameObject.AddComponent<CCRemoteConfig>();
        }
    }
}
