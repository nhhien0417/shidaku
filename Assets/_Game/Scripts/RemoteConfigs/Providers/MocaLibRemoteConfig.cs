using System;
using Titipi.MocaLib.Runtime.Services;
using UnityEngine;

namespace RemoteConfigs
{
    public class MocaLibRemoteConfig : IRemoteConfigProvider
    {
        private bool _isInitialized = false;

        public void Initialize(Action<bool> onRemoteConfigFetched, Action<bool> onRemoteConfigRefetched)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("MocaLibRemoteConfig is already initialized.");
                return;
            }
            _isInitialized = true;

            MocaLib.Instance.OnRemoteConfigFetchCompleted += onRemoteConfigFetched;
            MocaLib.Instance.EnableFirebaseRemoteConfig(true);
        }

        public T GetConfig<T>(string key, T defaultValue)  where T : IConvertible
        {
            var manager = MocaLib.Instance?.RemoteConfigManager;
            if (manager == null)
                return defaultValue;

            switch (defaultValue)
            {
                case double d:
                    return (T)(object)manager.GetRemoteDouble(key, d);
                case float f:
                    return (T)(object)manager.GetRemoteFloat(key, f);
                case long l:
                    return (T)(object)manager.GetRemoteLong(key, l);
                case int i:
                    return (T)(object)manager.GetRemoteInt(key, i);
                case bool b:
                    return (T)(object)manager.GetRemoteBool(key, b);
                case string s:
                    return (T)(object)manager.GetRemoteString(key, s);
                default:
                    Debug.LogError($"Unsupported type {typeof(T)}");
                    return defaultValue;
            }
        }
    }
}
