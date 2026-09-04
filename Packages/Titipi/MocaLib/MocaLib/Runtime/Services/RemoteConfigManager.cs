using System;
using UnityEngine;

using Titipi.MocaLib.Runtime.Common;
using Titipi.MocaLib.Runtime.Services.Internal;

namespace Titipi.MocaLib.Runtime.Services
{
    public class RemoteConfigManager : MonoBehaviour
    {
        private const string TAG = "RemoteConfigManager";
        
        private IRemoteConfigService _service;

        public void Initialize(IRemoteConfigService service, Action<bool> onFetchCompleted)
        {
#if UNITY_WEBGL
            return;
#endif
            
            Utils.MocaLibLog(TAG, "Initialize");

            _service = service;

            _service.OnFetchCompleted += onFetchCompleted;
            _service.Initialize();
        }

        public double GetRemoteDouble(string key, double defaultValue)
        {
#if UNITY_WEBGL
            return 0f;
#endif

            return _service.GetRemoteDouble(key, defaultValue);
        }

        public float GetRemoteFloat(string key, float defaultValue)
        {
#if UNITY_WEBGL
            return 0f;
#endif

            return _service.GetRemoteFloat(key, defaultValue);
        }

        public long GetRemoteLong(string key, long defaultValue)
        {
#if UNITY_WEBGL
            return 0;
#endif

            return _service.GetRemoteLong(key, defaultValue);
        }

        public int GetRemoteInt(string key, int defaultValue)
        {
#if UNITY_WEBGL
            return 0;
#endif

            return _service.GetRemoteInt(key, defaultValue);
        }

        public bool GetRemoteBool(string key, bool defaultValue)
        {
#if UNITY_WEBGL
            return false;
#endif

            return _service.GetRemoteBool(key, defaultValue);
        }

        public string GetRemoteString(string key, string defaultValue)
        {
#if UNITY_WEBGL
            return "";
#endif

            return _service.GetRemoteString(key, defaultValue);
        }
    }
}
