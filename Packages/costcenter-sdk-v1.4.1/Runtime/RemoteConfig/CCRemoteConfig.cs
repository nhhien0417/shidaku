using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.RemoteConfig;
using Newtonsoft.Json;

namespace CostCenter.RemoteConfig {
    public class CCRemoteConfig : MonoBehaviour
    {
        public static CCRemoteConfig instance;

        protected static bool _isConversionDataGet = false;
        public static Dictionary<string, object> ConversionData {
            get;
            protected set;
        }

        protected static readonly string[] DEFAULT_CONVERSION_FIELDS = new string[] {
            "media_source",
            "install_time",
            "af_siteid",
            "adgroup_id",
            "adset",
            "adset_id",
            "campaign_id",
            "campaign"
        };
        [SerializeField] private string[] _conversionFields = DEFAULT_CONVERSION_FIELDS;
        [Tooltip("Support for A/B Testing (only on first open)")]
        [SerializeField] private bool _autoReFetchRemoteConfig = false;

        public static Action<bool> OnFetchRemoteConfig;

        protected static bool _isFetchingRemoteConfig = false;

        void Awake() {
            instance = this;
            ConversionData = CCConversionData.Load();
        }

        public void ResetDefaultValues() {
            _conversionFields = DEFAULT_CONVERSION_FIELDS;
        }

        public void OnConversionDataSuccess(Dictionary<string, object> conversionData)
        {
            // Nếu đã nhận conversion data rồi thì không nhận nữa
            if (_isConversionDataGet)
            {
                return;
            }

            _isConversionDataGet = true;
            // Debug.Log("onConversionDataSuccess: " + conversionData);

            if (conversionData == null || conversionData.Count < 1)
            {
                return;
            }

            ConversionData = conversionData;

            CCConversionData.Save(conversionData);

            StartCoroutine(ApplyUserProperties(conversionData));
        }

        private IEnumerator ApplyUserProperties(Dictionary<string, object> conversionData)
        {
            yield return new WaitUntil(() => CCFirebase.IsInitialized);

            // Loop qua các cặp data có trong conversion data để set user property
            foreach (var pair in conversionData)
            {
                if (_conversionFields.Contains(pair.Key))
                {
                    var value = string.Empty;
                    if (pair.Value != null)
                    {
                        value = pair.Value.ToString();
                    }
                    Firebase.Analytics.FirebaseAnalytics.SetUserProperty(pair.Key, value);
                }
            }

            if (_autoReFetchRemoteConfig && CCConstant.IsFirstOpen)
            {
                AutoReFetchRemoteConfig();
            }
        }

        private IEnumerator IFetchRemoteConfig(bool waitIfFetching)
        {
            var waitFetchingTimeout = 60f;
            if (_isFetchingRemoteConfig)
            {
                if (waitIfFetching)
                {
                    while (_isFetchingRemoteConfig && waitFetchingTimeout > 0f)
                    {
                        yield return new WaitForEndOfFrame();
                        waitFetchingTimeout -= Time.unscaledDeltaTime;
                    }

                    if (waitFetchingTimeout <= 0f)
                        yield break;
                }
                else
                {
                    yield break;
                }
            }
            _isFetchingRemoteConfig = true;

            yield return new WaitUntil(() => CCFirebase.IsInitialized);

            var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
            var taskSettings = remoteConfig.SetConfigSettingsAsync(new ConfigSettings
            {
                FetchTimeoutInMilliseconds = FirebaseRemoteConfig.DefaultTimeoutInMilliseconds,
                MinimumFetchIntervalInMilliseconds = (ulong)FirebaseRemoteConfig.DefaultCacheExpiration.TotalMilliseconds
            });
            yield return new WaitUntil(() => taskSettings.IsCompleted);
            LogTaskError(taskSettings, "SetConfigSettingsAsync");

            var taskFetch = remoteConfig.FetchAsync(TimeSpan.Zero);
            yield return new WaitUntil(() => taskFetch.IsCompleted);

            var fetchHasTaskError = LogTaskError(taskFetch, "FetchAsync");
            var fetchInfo = remoteConfig.Info;
            if (!fetchHasTaskError && fetchInfo.LastFetchStatus == LastFetchStatus.Failure)
            {
                Debug.LogError(
                    $"[CCRemoteConfig] FetchAsync completed with failure. " +
                    $"Reason: {fetchInfo.LastFetchFailureReason}, " +
                    $"ThrottledEndTime: {fetchInfo.ThrottledEndTime}");
            }

            var taskActivate = remoteConfig.ActivateAsync();
            yield return new WaitUntil(() => taskActivate.IsCompleted);

            var activateHasTaskError = LogTaskError(taskActivate, "ActivateAsync");
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!activateHasTaskError && !taskActivate.Result)
            {
                Debug.LogWarning(
                    "[CCRemoteConfig] ActivateAsync completed but no new config was activated. " +
                    "The fetched config may already be active.");
            }
            #endif

            _isFetchingRemoteConfig = false;
            OnFetchRemoteConfig?.Invoke(FirebaseRemoteConfig.DefaultInstance.Info.LastFetchStatus == LastFetchStatus.Success);
        }

        private static bool LogTaskError(Task task, string taskName)
        {
            if (task.IsFaulted)
            {
                Debug.LogError(
                    $"[CCRemoteConfig] {taskName} failed:\n" +
                    $"{task.Exception?.Flatten()}");
                return true;
            }

            if (task.IsCanceled)
            {
                Debug.LogError($"[CCRemoteConfig] {taskName} was canceled.");
                return true;
            }

            return false;
        }

        public void FetchRemoteConfig()
        {
            StartCoroutine(IFetchRemoteConfig(false));
        }

        private void AutoReFetchRemoteConfig()
        {
            StartCoroutine(IFetchRemoteConfig(true));
        }

        public object GetDataByConversion(string key) {
            if (ConversionData == null || ConversionData.Count <= 0) {
                return null;
            }

            string stringValue = FirebaseRemoteConfig.DefaultInstance.GetValue($"cc__{key}").StringValue;
            if (string.IsNullOrEmpty(stringValue)) {
                return null;
            }

            try
            {
                CCConversionConfig[] configures = JsonConvert.DeserializeObject<CCConversionConfig[]>(stringValue);
                if (configures == null || configures.Length <= 0)
                {
                    return null;
                }

                foreach (var config in configures)
                {
                    if (config.IsMapWithConversionData(ConversionData))
                    {
                        return config.value;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing conversion config for key '{key}', value: {stringValue}: {ex.Message}");
            }
            return null;
        }

        public string GetStringValue(string key) {
            string stringValue = FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
            object valueByConversion = GetDataByConversion(key);
            // Debug.Log($"GetStringValue - Conversion: {valueByConversion} - Value: {stringValue}");
            return valueByConversion != null ? valueByConversion.ToString() : stringValue;
        }

        public bool GetBooleanValue(string key) {
            bool booleanValue = FirebaseRemoteConfig.DefaultInstance.GetValue(key).BooleanValue;
            object valueByConversion = GetDataByConversion(key);
            // Debug.Log($"GetBooleanValue - Conversion: {valueByConversion} - Value: {booleanValue}");
            if (valueByConversion != null)
            {
                if (bool.TryParse(valueByConversion.ToString(), out var result))
                {
                    return result;
                }
            }
            return booleanValue;
        }

        public long GetLongValue(string key) {
            long longValue = FirebaseRemoteConfig.DefaultInstance.GetValue(key).LongValue;
            object valueByConversion = GetDataByConversion(key);
            // Debug.Log($"GetLongValue - Conversion: {valueByConversion} - Value: {longValue}");
            if (valueByConversion != null)
            {
                if (long.TryParse(valueByConversion.ToString(), out var result))
                {
                    return result;
                }
            }
            return longValue;
        }

        public double GetDoubleValue(string key) {
            double doubleValue = FirebaseRemoteConfig.DefaultInstance.GetValue(key).DoubleValue;
            object valueByConversion = GetDataByConversion(key);
            // Debug.Log($"GetDoubleValue - Conversion: {valueByConversion} - Value: {doubleValue}");
            if (valueByConversion != null)
            {
                if (double.TryParse(valueByConversion.ToString(), out var result))
                {
                    return result;
                }
            }
            return doubleValue;
        }
    }
}
