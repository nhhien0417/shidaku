using System;
using System.Globalization;
using CostCenter.RemoteConfig;
using Firebase.RemoteConfig;
using UnityEngine;

namespace RemoteConfigs
{
    public class CostCenterRemoteConfig : IRemoteConfigProvider
    {
        private Action<bool> _onRemoteConfigFetched;
        private Action<bool> _onRemoteConfigRefetched;

        private bool _remoteConfigHasBeenFetched = false;
        private bool _isInitialized = false;

        ~CostCenterRemoteConfig()
        {
            CCRemoteConfig.OnFetchRemoteConfig -= OnFetchRemoteConfig;
        }

        public void Initialize(Action<bool> onRemoteConfigFetched, Action<bool> onRemoteConfigRefetched)
        {
            _remoteConfigHasBeenFetched = false;
            _onRemoteConfigFetched = onRemoteConfigFetched;
            _onRemoteConfigRefetched = onRemoteConfigRefetched;

            if (_isInitialized)
                return;

            CCRemoteConfig.OnFetchRemoteConfig += OnFetchRemoteConfig;
            CCRemoteConfig.instance?.FetchRemoteConfig();
            _isInitialized = true;
        }

        public T GetConfig<T>(string key, T defaultValue) where T : IConvertible
        {
            var manager = CCRemoteConfig.instance;
            if (manager != null)
            {
                // Conversion value
                try
                {
                    var valueByConversion = manager.GetDataByConversion(key);
                    if (valueByConversion != null)
                    {
                        var rawValue = Convert.ToString(valueByConversion, CultureInfo.InvariantCulture);
                        if (!string.IsNullOrEmpty(rawValue))
                        {
                            Debug.Log($"[CostCenterRemoteConfig] GetConfig -> Found conversion value for key '{key}': {rawValue}");
                            return (T)Convert.ChangeType(rawValue, typeof(T), CultureInfo.InvariantCulture);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"[CostCenterRemoteConfig] Failed to convert remote config value for key '{key}' to type '{typeof(T)}': {e.Message}");
                }
            }

            // Firebase value
            try
            {
                var configValue = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
                if (configValue.Source != ValueSource.StaticValue)
                {
                    switch (defaultValue)
                    {
                        case double d:
                            return (T)(object)configValue.DoubleValue;
                        case float f:
                            return (T)(object)(float)configValue.DoubleValue;
                        case long l:
                            return (T)(object)configValue.LongValue;
                        case int i:
                            return (T)(object)(int)configValue.LongValue;
                        case bool b:
                            return (T)(object)configValue.BooleanValue;
                        case string s:
                            return (T)(object)(string.IsNullOrEmpty(configValue.StringValue) ? defaultValue : configValue.StringValue);
                        default:
                            Debug.LogError($"[CostCenterRemoteConfig] Unsupported type {typeof(T)}");
                            return defaultValue;
                    }
                }
                else
                {
                    Debug.LogWarning($"[CostCenterRemoteConfig] GetRemote -> Cannot get `{key}` from Firebase");
                }

            }
            catch (Exception e)
            {
                Debug.LogError($"[CostCenterRemoteConfig] GetRemoteDouble -> Exception: {e}");
            }

            // Default value
            return defaultValue;
        }

        private void OnFetchRemoteConfig(bool success)
        {
            Debug.Log($"[CostCenterRemoteConfig] Remote config fetched {(success ? "SUCCESS" : "FAILED")}");

            if (!_remoteConfigHasBeenFetched)
            {
                _remoteConfigHasBeenFetched = true;
                _onRemoteConfigFetched?.Invoke(success);
            }
            else
            {
                _onRemoteConfigRefetched?.Invoke(success);
            }
        }
    }
}
