using System;
using System.Collections.Generic;
using _Game.Scripts.LiveOps;
using _Game.Scripts.Utils;
using Design;
using MEC;
using SimpleJSON;
using Titipi.MocaLib.Runtime.Services;
using UnityEngine;
using UserDataPack;

namespace RemoteConfigs
{
    public class RemoteConfigHelper : SingletonComponent<RemoteConfigHelper>
    {
        private IRemoteConfigProvider _remoteConfigProvider;
        #if UNITY_EDITOR
        [SerializeField] private List<LocalConfig> _localConfigs = new ();
        #endif

        public void Initialize(Action<bool> onRemoteConfigFetched)
        {
            _remoteConfigProvider = new CostCenterRemoteConfig();
            _remoteConfigProvider.Initialize(onRemoteConfigFetched, OnRemoteConfigRefetched);
        }

        public T GetConfig<T>(string key, T defaultValue) where T : IConvertible
        {
            #if UNITY_EDITOR
            var localConfig = _localConfigs.Find(config => config.Key == key);
            if (localConfig.Key != null)
            {
                try
                {
                    return (T)Convert.ChangeType(localConfig.Value, typeof(T));
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to convert local config value for key '{key}' to type '{typeof(T)}': {e.Message}");
                    return defaultValue;
                }
            }
            #endif

            if (_remoteConfigProvider == null)
            {
                Debug.LogError("RemoteConfigHelper is not initialized.");
                return defaultValue;
            }

            return _remoteConfigProvider.GetConfig(key, defaultValue);
        }

        public T GetAdConfig<T>(string key, T defaultValue) where T : IConvertible
        {
            var adConfig = GetCollectivelyAdConfig();
            return GetAdConfigValue(adConfig, key, defaultValue);
        }

        public void ApplyRemoteConfigs()
        {
            var adConfig = GetCollectivelyAdConfig();
            ApplyAdRemoteConfigs(adConfig);
            Timing.RunCoroutine(ApplyAdRemoteConfigsIE(adConfig));

            Game.Leaderboard.LeaderboardManager.ApplyRemoteConfig();
            AppRatingHelper.LevelToShowRating = GetConfig(RemoteConfigKey.SHOW_RATING_POPUP_AT_LEVEL, AppRatingHelper.LevelToShowRating);
            DataHelper.DefaultEnableBorders = GetConfig(RemoteConfigKey.DEFAULT_ENABLE_BORDERS, DataHelper.DefaultEnableBorders);

            DesignDataHolder.Instance?.ApplyRemoteConfig();

            // Apply some features need to be applied after remote config fetch completed
            TotalPlaytimeTracker.ManualSyncUserProperty(); // Make sure to sync the total playtime user property after remote config fetch completed, in case the playtime group config is updated from remote config
            UserProperty.SetCurrentLevel(UserData.Instance.GameplayData.CurrentGameplayLevel);
        }

        private void ApplyAdRemoteConfigs(JSONObject adConfig)
        {
            AdsManager.LevelToShowInterstitialAd = GetAdConfigValue(adConfig, RemoteConfigKey.INTER_START_FROM_LEVEL, AdsManager.LevelToShowInterstitialAd);
            AdsManager.LevelsEndedToShowInterstitialAd = GetAdConfigValue(adConfig, RemoteConfigKey.INTER_SHOW_AFTER_X_LEVELS, AdsManager.LevelsEndedToShowInterstitialAd);
            AdsManager.AutoTriggerInterEndgameInterval = GetAdConfigValue(adConfig, RemoteConfigKey.AUTO_TRIGGER_ENDGAME_INTERVAL, AdsManager.AutoTriggerInterEndgameInterval);
            AdsManager.ShowBannerAtLevel = GetAdConfigValue(adConfig, RemoteConfigKey.SHOW_BANNER_AT_LEVEL, AdsManager.ShowBannerAtLevel);
            AdsManager.AllowMultipleRewardWithRvAtLevel = GetAdConfigValue(adConfig, RemoteConfigKey.ALLOW_MULTIPLE_REWARD_WITH_RV_AFTER_LEVEL, AdsManager.AllowMultipleRewardWithRvAtLevel);
            AdsManager.ShowAdBreakAfterXSeconds = GetAdConfigValue(adConfig, RemoteConfigKey.SHOW_AD_BREAK_AFTER_X_SECONDS, AdsManager.ShowAdBreakAfterXSeconds);
            AdsManager.ShowInterstitialOnChangeScene = GetAdConfigValue(adConfig, RemoteConfigKey.INTER_SHOW_ON_CHANGE_SCENE, AdsManager.ShowInterstitialOnChangeScene);

            var requireInternetConfig = GetAdConfigValue(adConfig, RemoteConfigKey.REQUIRED_INTERNET, InternetCheckerWithRemoteConfig.RemoteRequireInternetConfig);
            if (InternetChecker.Instance is InternetCheckerWithRemoteConfig internetChecker)
            {
                _ = internetChecker.ApplyRemoteConfig(requireInternetConfig);
            }
        }

        private IEnumerator<float> ApplyAdRemoteConfigsIE(JSONObject adRemoteConfig = null)
        {
            var adConfig = MocaLib.Instance?.AdManager?.AdConfig;
            var timeout = 30f;
            while (adConfig == null && timeout > 0)
            {
                yield return Timing.WaitForOneFrame;
                adConfig = MocaLib.Instance?.AdManager?.AdConfig;
                timeout -= Time.deltaTime;
            }

            if (adConfig != null)
            {
                adConfig.InterstitialInterval = GetAdConfigValue(adRemoteConfig, RemoteConfigKey.INTERSTITIAL_INTERVAL, adConfig.InterstitialInterval);
                adConfig.IsAppOpenAdEnabled = GetAdConfigValue(adRemoteConfig, RemoteConfigKey.APP_OPEN_AD_ENABLED, adConfig.IsAppOpenAdEnabled);
            }

            if (MocaLib.Instance?.AdManager != null)
            {
                MocaLib.Instance.AdManager.IsMultipleAdUnitsEnabled = GetAdConfigValue(adRemoteConfig, RemoteConfigKey.MULTI_AD_UNITS_ENABLED, MocaLib.Instance.AdManager.IsMultipleAdUnitsEnabled);
            }
        }

        private JSONObject GetCollectivelyAdConfig()
        {
            var adConfig = new JSONObject();
            var adConfigString = GetConfig(RemoteConfigKey.AD_CONFIG, "");
            if (!string.IsNullOrEmpty(adConfigString))
            {
                try
                {
                    adConfig = JSON.Parse(adConfigString) as JSONObject;
                    if (adConfig == null)
                        adConfig = new();

                }
                catch (Exception e)
                {
                    Debug.LogError($"[RemoteConfigHelper] Failed to parse remote config: {e.Message}");
                    adConfig = new();
                }
            }

            return adConfig;
        }

        private T GetAdConfigValue<T>(JSONObject adConfig, string key, T defaultValue) where T : IConvertible
        {
            if (adConfig != null && adConfig.HasKey(key))
            {
                try
                {
                    return (T)Convert.ChangeType(adConfig[key].Value, typeof(T));
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to convert ad config value for key '{key}' to type '{typeof(T)}': {e.Message}");
                }
            }

            return GetConfig(key, defaultValue);
        }

        private void OnRemoteConfigRefetched(bool success)
        {
            ApplyRemoteConfigs();
            if (success)
            {
                Debug.Log("[RemoteConfigHelper] Remote config fetched successfully.");
            }
            else
            {
                Debug.LogError("[RemoteConfigHelper] Failed to fetch remote config.");
            }
        }

        #if UNITY_EDITOR
        [Serializable]
        private struct LocalConfig
        {
            public string Key;
            public string Value;
        }
        #endif
    }
}
