using System;
using System.Collections;
using UnityEngine;

#if !UNITY_WEBGL
using Firebase.Extensions;
using Firebase.RemoteConfig;
#endif

using Titipi.MocaLib.Runtime.Common;
using Titipi.MocaLib.Runtime.Services.Internal;

namespace Titipi.MocaLib.Runtime.Services
{
    public class FirebaseRemoteConfigService : MonoBehaviour, IRemoteConfigService
    {
        private const string TAG = "FirebaseRemoteConfigService";

        private float _fetchRequestedTime;

        public event Action<bool> OnFetchCompleted;

        public void Initialize()
        {
#if !UNITY_WEBGL
            Utils.MocaLibLog(TAG, "Initialize");
            StartCoroutine(FetchRemoteConfig(status => Utils.MocaLibLog(TAG, status)));
#endif
        }

#if !UNITY_WEBGL
        private IEnumerator FetchRemoteConfig(Action<string> status)
        {
            yield return new WaitForEndOfFrame();

            _fetchRequestedTime = Time.unscaledTime;

            FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero).ContinueWithOnMainThread(_ =>
            {
                var t = Time.unscaledTime - _fetchRequestedTime;
                var info = FirebaseRemoteConfig.DefaultInstance.Info;
                
                switch (info.LastFetchStatus)
                {
                    case LastFetchStatus.Success:
                        status?.Invoke($"Fetch result: Success. Fetch time = {t} seconds");
                        break;

                    case LastFetchStatus.Failure:
                        switch (info.LastFetchFailureReason)
                        {
                            case FetchFailureReason.Error:
                                status?.Invoke($"Fetch result: Error. Fetch time = {t} seconds");
                                break;

                            case FetchFailureReason.Throttled:
                                status?.Invoke($"Fetch result: Throttled until {info.ThrottledEndTime}. Fetch time = {t} seconds");
                                break;
                        }
                        break;

                    case LastFetchStatus.Pending:
                        status?.Invoke("Fetch result: Pending. Fetch time = {t} seconds");
                        break;
                }

                if (info.LastFetchStatus == LastFetchStatus.Success)
                {
                    FirebaseRemoteConfig.DefaultInstance.ActivateAsync().ContinueWithOnMainThread(_ =>
                    {
                        OnFetchCompleted?.Invoke(true);
                    });
                }
                else
                {
                    OnFetchCompleted?.Invoke(false);
                }
            });
        }
#endif

        public double GetRemoteDouble(string key, double defaultValue)
        {
#if UNITY_WEBGL
            return 0f;
#else
            try
            {
                var v = FirebaseRemoteConfig.DefaultInstance.GetValue(key);

                if (v.Source != ValueSource.StaticValue) return v.DoubleValue;

                Utils.MocaLibLogWarning(TAG, $"GetRemoteDouble -> Cannot get `{key}` from Firebase");
            }
            catch (Exception e)
            {
                Utils.MocaLibLogError(TAG, $"GetRemoteDouble -> Exception: {e}");
                return defaultValue;
            }

            return defaultValue;
#endif
        }

        public float GetRemoteFloat(string key, float defaultValue)
        {
#if UNITY_WEBGL
            return 0f;
#else
            try
            {
                var v = FirebaseRemoteConfig.DefaultInstance.GetValue(key);

                if (v.Source != ValueSource.StaticValue) return (float)v.DoubleValue;

                Utils.MocaLibLogWarning(TAG, $"GetRemoteFloat -> Cannot get `{key}` from Firebase");
            }
            catch (Exception e)
            {
                Utils.MocaLibLogError(TAG, $"GetRemoteFloat -> Exception: {e}");
                return defaultValue;
            }

            return defaultValue;
#endif
        }

        public long GetRemoteLong(string key, long defaultValue)
        {
#if UNITY_WEBGL
            return 0;
#else
            try
            {
                var v = FirebaseRemoteConfig.DefaultInstance.GetValue(key);

                if (v.Source != ValueSource.StaticValue) return v.LongValue;

                Utils.MocaLibLogWarning(TAG, $"GetRemoteLong -> Cannot get `{key}` from Firebase");
            }
            catch (Exception e)
            {
                Utils.MocaLibLogError(TAG, $"GetRemoteLong -> Exception: {e}");
                return defaultValue;
            }

            return defaultValue;
#endif
        }

        public int GetRemoteInt(string key, int defaultValue)
        {
#if UNITY_WEBGL
            return 0;
#else
            try
            {
                var v = FirebaseRemoteConfig.DefaultInstance.GetValue(key);

                if (v.Source != ValueSource.StaticValue) return (int)v.LongValue;

                Utils.MocaLibLogWarning(TAG, $"GetRemoteInt -> Cannot get `{key}` from Firebase");
            }
            catch (Exception e)
            {
                Utils.MocaLibLogError(TAG, $"GetRemoteInt -> Exception: {e}");
                return defaultValue;
            }

            return defaultValue;
#endif
        }

        public bool GetRemoteBool(string key, bool defaultValue)
        {
#if UNITY_WEBGL
            return false;
#else
            try
            {
                var v = FirebaseRemoteConfig.DefaultInstance.GetValue(key);

                if (v.Source != ValueSource.StaticValue) return v.BooleanValue;

                Utils.MocaLibLogWarning(TAG, $"GetRemoteBool -> Cannot get `{key}` from Firebase");
            }
            catch (Exception e)
            {
                Utils.MocaLibLogError(TAG, $"GetRemoteBool -> Exception: {e}");
                return defaultValue;
            }

            return defaultValue;
#endif
        }

        public string GetRemoteString(string key, string defaultValue)
        {
#if UNITY_WEBGL
            return null;
#else
            try
            {
                var v = FirebaseRemoteConfig.DefaultInstance.GetValue(key);

                if (v.Source != ValueSource.StaticValue)
                {
                    var s = v.StringValue;
                    return string.IsNullOrEmpty(s) ? defaultValue : s;
                }

                Utils.MocaLibLogWarning(TAG, $"GetRemoteString -> Cannot get `{key}` from Firebase");
            }
            catch (Exception e)
            {
                Utils.MocaLibLogError(TAG, $"GetRemoteString -> Exception: {e}");
                return defaultValue;
            }

            return defaultValue;
#endif
        }
    }
}
