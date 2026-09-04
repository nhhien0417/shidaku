using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

#if UNITY_IOS && !UNITY_EDITOR
using Unity.Advertisement.IosSupport;
#endif

#if ADMANAGER_USE_GOOGLE_UMP
using GoogleMobileAds.Ump.Api;
#endif

using Titipi.MocaLib.Runtime.Common;
using Titipi.MocaLib.Runtime.Services.Internal;

namespace Titipi.MocaLib.Runtime.Services
{
    public enum AdCallbackType
    {
        AdLoaded,
        AdLoadFailed,
        AdDisplayed,
        AdDisplayFailed,
        AdClosed
    }

    public class AdManager : MonoBehaviour
    {
        private const string TAG = "AdManager";

        protected AdConfig _adConfigAndroid;
        protected AdConfig _adConfigIOS;

        // -------------------------------------------------------------------------------------------------------------

        [NonSerialized] public AdConfig AdConfig;

        private int _currentAppOpenIndex;
        private int _currentBannerIndex;

        private string CurrentAppOpenId
        {
            get => AdConfig.IsAppOpenAdEnabled ? GetAdIdAtIndex(AdConfig.AppOpenAdIds, _currentAppOpenIndex) : "";
            set
            {
                if (value != null) AdConfig.AppOpenAdIds[_currentAppOpenIndex] = value;
            }
        }

        private string CurrentBannerId
        {
            get => GetAdIdAtIndex(AdConfig.BannerAdIds, _currentBannerIndex);
            set => AdConfig.BannerAdIds[_currentBannerIndex] = value ?? throw new ArgumentNullException(nameof(value));
        }

        // Safely read an ad unit id by index. Guards against empty/null lists and out-of-range
        // indices (e.g. IsXxxAdEnabled toggled true via remote config while the id list is empty),
        // which would otherwise throw ArgumentOutOfRangeException.
        private static string GetAdIdAtIndex(List<string> ids, int index)
        {
            if (ids == null || index < 0 || index >= ids.Count) return "";
            return ids[index];
        }

        private DateTime _lastInterstitialShownTime = DateTime.MinValue;

        public bool IsMultipleAdUnitsEnabled = false;

        private List<AdUnitState> _interstitialUnits;
        private List<AdUnitState> _rewardedUnits;

        private float _nextReadyCheckTime;
        private const float ReadyCheckInterval = 1f;

        private event Action<bool> _rewardedAdAvailabilityEvent;

        private event Action _appOpenOnAdLoadedEvent;
        private event Action _appOpenOnAdLoadFailedEvent;
        private event Action _appOpenOnAdDisplayedEvent;
        private event Action _appOpenOnAdDisplayFailedEvent;
        private event Action _appOpenOnAdClosedEvent;

        private event Action _bannerOnAdLoadedEvent;
        private event Action _bannerOnAdFailedToLoadEvent;
        private event Action _bannerOnAdDisplayedEvent;
        private event Action _bannerOnAdDisplayFailedEvent;
        private event Action _bannerOnAdClosedEvent;

        private event Action _interstitialOnAdLoadedEvent;
        private event Action _interstitialOnAdFailedToLoadEvent;
        private event Action _interstitialOnAdDisplayedEvent;
        private event Action _interstitialOnAdDisplayFailedEvent;
        private event Action _interstitialOnAdClosedEvent;

        private event Action _rewardedAdLoadedEvent;
        private event Action _rewardedAdFailedToLoadEvent;
        private event Action _rewardedAdDisplayedEvent;
        private event Action _rewardedAdDisplayFailedEvent;
        private event Action _rewardedAdClosedEvent;

        private bool _shouldRewardUser;
        private Action<bool, int> _onRewardedAdClosed;
        private Action<bool, int> _onInterstitialAdClosed;

        private bool _isAppOpenAdAllowedToShow = true;
        private DateTime _appOpenAdCooldownTime = DateTime.MinValue;

        private int _appOpenAdsShown;
        private int _bannerAdsShown;
        private int _interstitialAdsShown;
        private int _rewardedAdsShown;

        private DateTime _adStartTime;
        private int _adShownDuration;

        // [ANR-PATCH] AOA load guard + backoff
        private bool _isAppOpenLoadInFlight;
        private int _appOpenRetryAttempt;

        // [ANR-PATCH] tuning constants
        private const int AppOpenMaxBackoffExp = 6;      // 2^6 = 64s cap
        private const float ReloadAfterCloseDelay = 0.5f; // reload after an ad is closed
        private const float DisplayFailReloadDelay = 1f;  // reload after a display failure

        private AdService _adService;

        public bool IsInitialized => _adService != null;

        private enum BannerAdStatus
        {
            Unknown,
            Showing,
            Hidden,
            HiddenOnApplicationPause
        }

        private BannerAdStatus _bannerAdStatus = BannerAdStatus.Unknown;

        // -------------------------------------------------------------------------------------------------------------

        public void Initialize(AdConfig adConfig, Action adEventsHandler = null)
        {
#if UNITY_WEBGL
            return;
#endif

            Utils.MocaLibLog(TAG, "Initialize");

            AdService service = null;

#if MOCALIB_AD_PROVIDER_LEVELPLAY
            service = new LevelPlayAdService();
#elif MOCALIB_AD_PROVIDER_APPLOVIN
            service = new AppLovinAdService();
#endif

            _adService = service ?? throw new Exception("[Titipi.MocaLib] [AdManager] Ad Provider is not set!");

            adEventsHandler?.Invoke();

#if ADMANAGER_USE_GOOGLE_UMP
            var request = new ConsentRequestParameters();
            ConsentInformation.Update(request, OnConsentInfoUpdated);
#else
            InitializeAds(adConfig);
#endif
        }

#if ADMANAGER_USE_GOOGLE_UMP
        // Ref: https://developers.google.com/admob/unity/privacy
        private void OnConsentInfoUpdated(FormError consentError)
        {
            if (consentError != null)
            {
                // Utils.MocaLibLogError(TAG, $"OnConsentInfoUpdated: {consentError.Message}");
                return;
            }

            // If the error is null, the consent information state was updated.
            // You are now ready to check if a form is available.
            ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
            {
                if (formError != null)
                {
                    // Utils.MocaLibLogError(TAG, "OnConsentInfoUpdated: Consent gathering failed");
                    return;
                }

                // Consent has been gathered...

                InitializeAds(adConfig)
            });
        }
#endif
        
        // -------------------------------------------------------------------------------------------------------------

        private void InitializeAds(AdConfig adConfig)
        {
            AdConfig = ScriptableObject.CreateInstance<AdConfig>();
            AdConfig = adConfig;

            VerifyAdUnits();

#if UNITY_IOS && !UNITY_EDITOR
            // Ref: https://developers.facebook.com/docs/audience-network/setting-up/platform-setup/ios/advertising-tracking-enabled
            if (Utils.Is_iOS_14_5_Or_Higher() && !Utils.Is_iOS_17_Or_Higher())
            {
                if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() == ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED)
                {
                    AudienceNetwork.AdSettings.SetAdvertiserTrackingEnabled(true);
                }
                else
                {
                    AudienceNetwork.AdSettings.SetAdvertiserTrackingEnabled(false);
                }
            }
#endif

            if (IsMultipleAdUnitsEnabled)
            {
                _adService.DisableB2B();
            }

            _adService.Initialize(
                AdConfig.AppKey,
                AdConfig.IsAppOpenAdEnabled,
                AdConfig.IsBannerAdEnabled,
                AdConfig.IsInterstitialAdEnabled,
                AdConfig.IsRewardedAdEnabled,
                AdConfig.UserId,
                OnSdkInitializedEvent);
        }

        private void OnSdkInitializedEvent()
        {
            if (AdConfig.IsAppOpenAdEnabled) InitializeAppOpenAd();
            if (AdConfig.IsBannerAdEnabled) InitializeBannerAd();
            if (AdConfig.IsInterstitialAdEnabled) InitializeInterstitialAd();
            if (AdConfig.IsRewardedAdEnabled) InitializeRewardedAd();
        }

        // -------------------------------------------------------------------------------------------------------------

        public void ShowAppOpenAd()
        {
#if UNITY_WEBGL
            return;
#endif

            if (AdConfig == null || !AdConfig.IsAppOpenAdEnabled) return;
            if (!_isAppOpenAdAllowedToShow) return;

            if ((DateTime.Now - _appOpenAdCooldownTime).TotalSeconds < 10) return;

            if (IsAppOpenAdReady())
            {
                _adService.ShowAppOpenAd(CurrentAppOpenId);
            }
            else
            {
                LoadAppOpenAd();
            }
        }

        public bool IsAppOpenAdReady()
        {
#if UNITY_WEBGL
            return false;
#endif

            return AdConfig !=null && AdConfig.IsAppOpenAdEnabled && _adService.IsAppOpenAdReady(CurrentAppOpenId);
        }

        // -------------------------------------------------------------------------------------------------------------

        public void ShowBannerAd()
        {
#if UNITY_WEBGL
            return;
#endif

            if (AdConfig == null || !AdConfig.IsBannerAdEnabled) return;

            _bannerAdStatus = BannerAdStatus.Showing;
            _adService.ShowBannerAd(CurrentBannerId);
        }

        public void HideBannerAd()
        {
#if UNITY_WEBGL
            return;
#endif

            if (AdConfig == null || !AdConfig.IsBannerAdEnabled) return;

            _bannerAdStatus = BannerAdStatus.Hidden;
            _adService.HideBannerAd(CurrentBannerId);
        }

        public float GetBannerHeight(Canvas canvas)
        {
#if UNITY_WEBGL
            return 0f;
#endif

            if (AdConfig == null || !AdConfig.IsBannerAdEnabled) return 0;

            var bannerHeight = _adService.GetBannerHeight();

            if (canvas != null)
            {
                bannerHeight /= canvas.scaleFactor;
            }

            return bannerHeight;
        }

        // -------------------------------------------------------------------------------------------------------------

        public void ShowInterstitialAd(Action<bool, int> onClosed = null)
        {
#if UNITY_WEBGL
            onClosed?.Invoke(false, 0);
            return;
#endif

            _adShownDuration = 0;

            if (AdConfig == null || !AdConfig.IsInterstitialAdEnabled)
            {
                onClosed?.Invoke(false, _adShownDuration);
                return;
            }

            _onInterstitialAdClosed = onClosed;

            if ((DateTime.Now - _lastInterstitialShownTime).TotalSeconds < AdConfig.InterstitialInterval)
            {
                onClosed?.Invoke(false, _adShownDuration);
                return;
            }

            var best = GetEligibleUnit(_interstitialUnits);
            if (best != null)
            {
                Utils.MocaLibLog(TAG, $"[MAU] Showing IS: id={best.AdUnitId} revenue=${best.Revenue:F6} primary={best.IsPrimary} MAU={IsMultipleAdUnitsEnabled}");
                AllowAppOpenAd(false);

                best.IsReady = false;
                _adStartTime = DateTime.Now;
                _adService.ShowInterstitialAd(best.AdUnitId);

                AllowAppOpenAd(true);
            }
            else
            {
                Utils.MocaLibLog(TAG, $"[MAU] IS not ready (MAU={IsMultipleAdUnitsEnabled})");
                onClosed?.Invoke(false, 0);
            }
        }

        public bool IsInterstitialAdReady()
        {
#if UNITY_WEBGL
            return false;
#endif

            if (AdConfig == null || !AdConfig.IsInterstitialAdEnabled) return false;
            return GetEligibleUnit(_interstitialUnits) != null;
        }

        // -------------------------------------------------------------------------------------------------------------

        public void ShowRewardedAd(Action<bool, int> onClosed)
        {
#if UNITY_WEBGL
            onClosed?.Invoke(false, 0);
            return;
#endif

            if (AdConfig == null || !AdConfig.IsRewardedAdEnabled)
            {
                Utils.MocaLibLogError(TAG, "Rewarded ad is disabled");
                return;
            }

            _adShownDuration = 0;

#if UNITY_EDITOR
            onClosed?.Invoke(true, 0);
#else
            _onRewardedAdClosed = onClosed;
            _shouldRewardUser = false;

            var best = GetEligibleUnit(_rewardedUnits);
            if (best != null)
            {
                Utils.MocaLibLog(TAG, $"[MAU] Showing RV: id={best.AdUnitId} revenue=${best.Revenue:F6} primary={best.IsPrimary} MAU={IsMultipleAdUnitsEnabled}");
                AllowAppOpenAd(false);

                best.IsReady = false;
                _adStartTime = DateTime.Now;
                _adService.ShowRewardedAd(best.AdUnitId);

                AllowAppOpenAd(true);
            }
            else
            {
                Utils.MocaLibLog(TAG, $"[MAU] RV not ready (MAU={IsMultipleAdUnitsEnabled})");
                onClosed?.Invoke(false, 0);
            }
#endif
        }

        public bool IsRewardedAdReady()
        {
#if UNITY_WEBGL
            return false;
#endif

            if (AdConfig == null || !AdConfig.IsRewardedAdEnabled) return false;
            return GetEligibleUnit(_rewardedUnits) != null;
        }

        public void RegisterOnRewardedAdAvailabilityChangedEvent(Action<bool> callback)
        {
#if UNITY_WEBGL
            return;
#endif

            _rewardedAdAvailabilityEvent += callback;
        }

        // -------------------------------------------------------------------------------------------------------------

        public void ShowIntegrationDebugger()
        {
#if UNITY_WEBGL
            return;
#endif

            _adService.ShowIntegrationDebugger();
        }

        private void VerifyAdUnits()
        {
            if (AdConfig == null)
            {
                throw new NullReferenceException("[Titipi.MocaLib] [AdManager] AdConfig is null!");
            }

            if (AdConfig.IsAppOpenAdEnabled && (AdConfig.AppOpenAdIds == null || AdConfig.AppOpenAdIds.Count == 0))
            {
                Utils.MocaLibLogWarning(TAG, "AppOpenAdIds not defined. App Open Ad will be disabled.");
                AdConfig.IsAppOpenAdEnabled = false;
            }

            if (AdConfig.IsBannerAdEnabled && (AdConfig.BannerAdIds == null || AdConfig.BannerAdIds.Count == 0))
            {
                Utils.MocaLibLogWarning(TAG, "BannerAdIds not defined. Banner Ad will be disabled.");
                AdConfig.IsBannerAdEnabled = false;
            }

            if (AdConfig.IsInterstitialAdEnabled && (AdConfig.InterstitialAdIds == null || AdConfig.InterstitialAdIds.Count == 0))
            {
                Utils.MocaLibLogWarning(TAG, "InterstitialAdIds not defined. Interstitial Ad will be disabled.");
                AdConfig.IsInterstitialAdEnabled = false;
            }

            if (AdConfig.IsRewardedAdEnabled && (AdConfig.RewardedAdIds == null || AdConfig.RewardedAdIds.Count == 0))
            {
                Utils.MocaLibLogWarning(TAG, "RewardedAdIds not defined. Rewarded Ad will be disabled.");
                AdConfig.IsRewardedAdEnabled = false;
            }
        }

        // -------------------------------------------------------------------------------------------------------------

        private void InitializeAppOpenAd()
        {
            _adService.AppOpenOnAdLoadedEvent = AppOpenOnAdLoadedEvent;
            _adService.AppOpenOnAdLoadFailedEvent = AppOpenOnAdLoadFailedEvent;
            _adService.AppOpenOnAdDisplayedEvent = AppOpenAdOnAdDisplayedEvent;
            _adService.AppOpenOnAdDisplayFailedEvent = AppOpenOnAdDisplayFailedEvent;
            _adService.AppOpenOnAdClosedEvent = AppOpenOnAdCloseEvent;

            _adService.InitializeAppOpenAdCallbacks();

            LoadAppOpenAd();
        }

        private void LoadAppOpenAd()
        {
            // [ANR-PATCH] avoid stacked loads (close + fail + retry at the same time)
            if (_isAppOpenLoadInFlight) return;
            _isAppOpenLoadInFlight = true;

            _adService.LoadAppOpenAd(CurrentAppOpenId);
        }

        public void AllowAppOpenAd(bool status)
        {
            if (status)
            {
                DOVirtual.DelayedCall(2, () => { _isAppOpenAdAllowedToShow = true; }, false);
            }
            else
            {
                _isAppOpenAdAllowedToShow = false;
            }
        }

        private void AppOpenOnAdLoadedEvent()
        {
            // [ANR-PATCH] load succeeded -> reset guard + counter
            _isAppOpenLoadInFlight = false;
            _appOpenRetryAttempt = 0;

            _appOpenOnAdLoadedEvent?.Invoke();
        }

        private void AppOpenOnAdLoadFailedEvent()
        {
            // [ANR-PATCH] backoff instead of immediate reload (avoid load->fail->load loop on no-fill/offline)
            _isAppOpenLoadInFlight = false;

            Utils.MocaLibLogError(TAG, "AppOpenAd failed to load");

            _appOpenOnAdLoadFailedEvent?.Invoke();

            _appOpenRetryAttempt++;
            float delay = Mathf.Pow(2, Mathf.Min(AppOpenMaxBackoffExp, _appOpenRetryAttempt)); // 2..64s
            DOVirtual.DelayedCall(delay, LoadAppOpenAd, true);
        }

        private void AppOpenAdOnAdDisplayedEvent()
        {
            _appOpenOnAdDisplayedEvent?.Invoke();
            _appOpenAdsShown++;
        }

        private void AppOpenOnAdDisplayFailedEvent()
        {
            // [ANR-PATCH] reset guard then reload with a small delay
            _isAppOpenLoadInFlight = false;

            Utils.MocaLibLogError(TAG, "AppOpenAd failed to display");

            _appOpenOnAdDisplayFailedEvent?.Invoke();
            DOVirtual.DelayedCall(ReloadAfterCloseDelay, LoadAppOpenAd, true);
        }

        private void AppOpenOnAdCloseEvent()
        {
            _appOpenOnAdClosedEvent?.Invoke();

            _appOpenAdCooldownTime = DateTime.Now;

            // [ANR-PATCH] reset guard + reload with a small delay to keep the ad-close frame smooth
            _isAppOpenLoadInFlight = false;
            DOVirtual.DelayedCall(ReloadAfterCloseDelay, LoadAppOpenAd, true);
        }

        // -------------------------------------------------------------------------------------------------------------

        private void InitializeBannerAd()
        {
            _adService.BannerOnAdLoadedEvent = BannerOnAdLoadedEvent;
            _adService.BannerOnAdLoadFailedEvent = BannerOnAdLoadFailedEvent;
            _adService.BannerOnAdDisplayedEvent = BannerOnAdDisplayedEvent;
            _adService.BannerOnAdDisplayFailedEvent = BannerOnAdDisplayFailedEvent;
            _adService.BannerOnAdClosedEvent = BannerOnAdClosedEvent;

            _adService.InitializeBannerAdCallbacks();

            LoadBannerAd();
        }

        private void LoadBannerAd()
        {
            _adService.LoadBannerAd(CurrentBannerId);
        }

        private void BannerOnAdLoadedEvent()
        {
            _bannerOnAdLoadedEvent?.Invoke();
        }

        private void BannerOnAdLoadFailedEvent()
        {
            Utils.MocaLibLogError(TAG, "BannerAd failed to load");

            _bannerOnAdFailedToLoadEvent?.Invoke();
        }

        private void BannerOnAdDisplayedEvent()
        {
            _bannerOnAdDisplayedEvent?.Invoke();
        }

        private void BannerOnAdDisplayFailedEvent()
        {
            Utils.MocaLibLogError(TAG, "BannerAd failed to display");

            _bannerOnAdDisplayFailedEvent?.Invoke();
        }

        private void BannerOnAdClosedEvent()
        {
            _bannerOnAdClosedEvent?.Invoke();
        }

        // -------------------------------------------------------------------------------------------------------------

        private void InitializeInterstitialAd()
        {
            _adService.InterstitialOnAdLoadedEvent = InterstitialOnAdLoadedEvent;
            _adService.InterstitialOnAdLoadFailedEvent = InterstitialOnAdLoadFailedEvent;
            _adService.InterstitialOnAdDisplayedEvent = InterstitialOnAdDisplayedEvent;
            _adService.InterstitialOnAdDisplayFailedEvent = InterstitialOnAdDisplayFailedEvent;
            _adService.InterstitialOnAdClosedEvent = InterstitialOnAdClosedEvent;

            _adService.InitializeInterstitialAdCallbacks();

            _interstitialUnits = CreateAdUnitStates(AdConfig.InterstitialAdIds);
            Utils.MocaLibLog(TAG, $"[MAU] IS units initialized: {_interstitialUnits.Count}, MAU={IsMultipleAdUnitsEnabled}");
            foreach (var u in _interstitialUnits)
                Utils.MocaLibLog(TAG, $"[MAU] IS  id={u.AdUnitId} primary={u.IsPrimary} loadDelay={u.TimeBeforeLoad}s maxAttempts={u.MaxReloadAttempts}");
        }

        private void InterstitialOnAdLoadedEvent(string adUnitId, double revenue)
        {
            var unit = FindUnitOrFirst(_interstitialUnits, adUnitId);
            if (unit != null)
            {
                unit.Revenue = revenue;
                unit.IsReady = true;
                unit.ReloadAfterFailAttempts = 0;
            }

            SortByRevenue(_interstitialUnits);
            Utils.MocaLibLog(TAG, $"[MAU] IS loaded: id={adUnitId} revenue=${revenue:F6} primary={unit?.IsPrimary}");
            _interstitialOnAdLoadedEvent?.Invoke();
        }

        private void InterstitialOnAdLoadFailedEvent(string adUnitId)
        {
            var unit = FindUnitOrFirst(_interstitialUnits, adUnitId);
            if (unit != null)
            {
                unit.IsReady = false;
                unit.ReloadAfterFailAttempts++;

                if (unit.MaxReloadAttempts >= 0 && unit.ReloadAfterFailAttempts >= unit.MaxReloadAttempts)
                {
                    _interstitialUnits.Remove(unit);
                    Utils.MocaLibLogError(TAG, $"[MAU] IS load failed: id={adUnitId} — removed after {unit.ReloadAfterFailAttempts} attempts");
                }
                else
                {
                    unit.TimeBeforeLoad = unit.IsDelayConstant
                        ? unit.GetConstantTimeBeforeReload()
                        : unit.GetIncreasingTimeBeforeReload();
                    Utils.MocaLibLogError(TAG, $"[MAU] IS load failed: id={adUnitId} attempt={unit.ReloadAfterFailAttempts} retryIn={unit.TimeBeforeLoad}s");
                }
            }
            else
            {
                Utils.MocaLibLogError(TAG, $"[MAU] IS load failed: id={adUnitId} (unit not found)");
            }

            _interstitialOnAdFailedToLoadEvent?.Invoke();
        }

        private void InterstitialOnAdDisplayedEvent(string adUnitId)
        {
            _interstitialOnAdDisplayedEvent?.Invoke();
            _interstitialAdsShown++;
        }

        private void InterstitialOnAdDisplayFailedEvent(string adUnitId)
        {
            Utils.MocaLibLogError(TAG, "InterstitialAd failed to display");

            _interstitialOnAdDisplayFailedEvent?.Invoke();

            var unit = FindUnitOrFirst(_interstitialUnits, adUnitId);
            if (unit != null)
            {
                unit.IsReady = false;
                // [ANR-PATCH] defer reload so we don't hammer the main thread on the display-fail frame
                DOVirtual.DelayedCall(DisplayFailReloadDelay, () => _adService.LoadInterstitialAd(unit.AdUnitId), true);
            }

            _onInterstitialAdClosed?.Invoke(false, _adShownDuration);
            _onInterstitialAdClosed = null;
        }

        private void InterstitialOnAdClosedEvent(string adUnitId)
        {
            _interstitialOnAdClosedEvent?.Invoke();

            var now = DateTime.Now;
            _lastInterstitialShownTime = now;
            _adShownDuration = (now - _adStartTime).Seconds;

            var unit = FindUnitOrFirst(_interstitialUnits, adUnitId);
            if (unit != null)
            {
                unit.IsReady = false;
                // [ANR-PATCH] defer reload to keep the ad-close frame smooth (reward animation)
                DOVirtual.DelayedCall(ReloadAfterCloseDelay, () => _adService.LoadInterstitialAd(unit.AdUnitId), true);
            }

            MocaLib.Instance.AnalyticsManager.LogEvent("interstitial_ad_closed");

            _onInterstitialAdClosed?.Invoke(true, _adShownDuration);
            _onInterstitialAdClosed = null;
        }

        // -------------------------------------------------------------------------------------------------------------

        private void InitializeRewardedAd()
        {
            _adService.RewardedOnAdLoadedEvent = RewardedOnAdLoadedEvent;
            _adService.RewardedOnAdLoadFailedEvent = RewardedOnAdLoadFailedEvent;
            _adService.RewardedOnAdDisplayedEvent = RewardedOnAdDisplayedEvent;
            _adService.RewardedOnAdDisplayFailedEvent = RewardedOnAdDisplayFailedEvent;
            _adService.RewardedOnAdClosedEvent = RewardedOnAdClosedEvent;
            _adService.RewardedOnAdReceivedRewardEvent = RewardedOnAdReceivedRewardEvent;

            _adService.InitializeRewardedAdCallbacks();

            _rewardedUnits = CreateAdUnitStates(AdConfig.RewardedAdIds);
            Utils.MocaLibLog(TAG, $"[MAU] RV units initialized: {_rewardedUnits.Count}, MAU={IsMultipleAdUnitsEnabled}");
            foreach (var u in _rewardedUnits)
                Utils.MocaLibLog(TAG, $"[MAU] RV  id={u.AdUnitId} primary={u.IsPrimary} loadDelay={u.TimeBeforeLoad}s maxAttempts={u.MaxReloadAttempts}");
        }

        private void RewardedOnAdLoadedEvent(string adUnitId, double revenue)
        {
            var unit = FindUnitOrFirst(_rewardedUnits, adUnitId);
            if (unit != null)
            {
                unit.Revenue = revenue;
                unit.IsReady = true;
                unit.ReloadAfterFailAttempts = 0;
            }

            SortByRevenue(_rewardedUnits);
            Utils.MocaLibLog(TAG, $"[MAU] RV loaded: id={adUnitId} revenue=${revenue:F6} primary={unit?.IsPrimary}");
            _rewardedAdLoadedEvent?.Invoke();
            _rewardedAdAvailabilityEvent?.Invoke(true);
        }

        private void RewardedOnAdLoadFailedEvent(string adUnitId)
        {
            var unit = FindUnitOrFirst(_rewardedUnits, adUnitId);
            if (unit != null)
            {
                unit.IsReady = false;
                unit.ReloadAfterFailAttempts++;

                if (unit.MaxReloadAttempts >= 0 && unit.ReloadAfterFailAttempts >= unit.MaxReloadAttempts)
                {
                    _rewardedUnits.Remove(unit);
                    Utils.MocaLibLogError(TAG, $"[MAU] RV load failed: id={adUnitId} — removed after {unit.ReloadAfterFailAttempts} attempts");
                }
                else
                {
                    unit.TimeBeforeLoad = unit.IsDelayConstant
                        ? unit.GetConstantTimeBeforeReload()
                        : unit.GetIncreasingTimeBeforeReload();
                    Utils.MocaLibLogError(TAG, $"[MAU] RV load failed: id={adUnitId} attempt={unit.ReloadAfterFailAttempts} retryIn={unit.TimeBeforeLoad}s");
                }
            }
            else
            {
                Utils.MocaLibLogError(TAG, $"[MAU] RV load failed: id={adUnitId} (unit not found)");
            }

            _rewardedAdFailedToLoadEvent?.Invoke();

            if (GetEligibleUnit(_rewardedUnits) == null)
                _rewardedAdAvailabilityEvent?.Invoke(false);
        }

        private void RewardedOnAdDisplayedEvent(string adUnitId)
        {
            _rewardedAdDisplayedEvent?.Invoke();
            _rewardedAdsShown++;
        }

        private void RewardedOnAdDisplayFailedEvent(string adUnitId)
        {
            Utils.MocaLibLogError(TAG, "RewardedAd failed to display");

            _rewardedAdDisplayFailedEvent?.Invoke();

            var unit = FindUnitOrFirst(_rewardedUnits, adUnitId);
            if (unit != null)
            {
                unit.IsReady = false;
                // [ANR-PATCH] defer reload so we don't hammer the main thread on the display-fail frame
                DOVirtual.DelayedCall(DisplayFailReloadDelay, () => _adService.LoadRewardedAd(unit.AdUnitId), true);
            }
            
            _onRewardedAdClosed?.Invoke(false, 0);
            _onRewardedAdClosed = null;
        }

        private void RewardedOnAdClosedEvent(string adUnitId)
        {
            _rewardedAdClosedEvent?.Invoke();

            var now = DateTime.Now;
            _lastInterstitialShownTime = now;
            _adShownDuration = _shouldRewardUser ? (now - _adStartTime).Seconds : 0;

            _onRewardedAdClosed?.Invoke(_shouldRewardUser, _adShownDuration);
            _onRewardedAdClosed = null;
            _shouldRewardUser = false;

            var unit = FindUnitOrFirst(_rewardedUnits, adUnitId);
            if (unit != null)
            {
                unit.IsReady = false;
                // [ANR-PATCH] defer reload to keep the ad-close frame smooth (reward animation)
                DOVirtual.DelayedCall(ReloadAfterCloseDelay, () => _adService.LoadRewardedAd(unit.AdUnitId), true);
            }
        }

        private void RewardedOnAdReceivedRewardEvent()
        {
            _shouldRewardUser = true;
        }

        // -------------------------------------------------------------------------------------------------------------

        private void Update()
        {
            if (_adService == null) return;

            TickAdUnits(_interstitialUnits, _adService.LoadInterstitialAd);
            TickAdUnits(_rewardedUnits, _adService.LoadRewardedAd);

            if (Time.time < _nextReadyCheckTime) return;
            _nextReadyCheckTime = Time.time + ReadyCheckInterval;

            PollReadyState(_interstitialUnits, _adService.IsInterstitialAdReady);
            PollReadyState(_rewardedUnits, _adService.IsRewardedAdReady);
        }

        private static void TickAdUnits(List<AdUnitState> units, Action<string> loadAction)
        {
            if (units == null) return;

            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit.TimeBeforeLoad < 0) continue;
                unit.TimeBeforeLoad -= Time.unscaledDeltaTime;
                if (unit.TimeBeforeLoad > 0) continue;
                loadAction(unit.AdUnitId);
            }
        }

        private static void PollReadyState(List<AdUnitState> units, Func<string, bool> isReadyFunc)
        {
            if (units == null) return;

            foreach (var unit in units)
            {
                if (unit.TimeBeforeLoad >= 0) continue; // still counting down or not yet triggered
                unit.IsReady = isReadyFunc(unit.AdUnitId);
            }
        }

        private static List<AdUnitState> CreateAdUnitStates(List<string> adUnitIds)
        {
            var preloadDelayPattern = new float[] { 0, 10, 30 };
            var maxReloadAttemptsPattern = new int[] { -1, -1, 3 };
            var reloadDelayPattern = new float[] { 64, 64, 30 };
            var constantReloadDelayPattern = new bool[] { false, false, true };

            var units = new List<AdUnitState>(adUnitIds.Count);
            for (var i = 0; i < adUnitIds.Count; i++)
            {
                var delay = preloadDelayPattern[Mathf.Min(i, preloadDelayPattern.Length - 1)];
                if (i > 0) delay += units[i - 1].TimeBeforeLoad;

                units.Add(new AdUnitState
                {
                    AdUnitId = adUnitIds[i],
                    Revenue = 0,
                    IsReady = false,
                    IsPrimary = i == 0,
                    TimeBeforeLoad = delay,
                    MaxReloadAttempts = maxReloadAttemptsPattern[Mathf.Min(i, maxReloadAttemptsPattern.Length - 1)],
                    ReloadDelay = reloadDelayPattern[Mathf.Min(i, reloadDelayPattern.Length - 1)],
                    IsDelayConstant = constantReloadDelayPattern[Mathf.Min(i, constantReloadDelayPattern.Length - 1)],
                    ReloadAfterFailAttempts = 0,
                });
            }
            return units;
        }

        private static void SortByRevenue(List<AdUnitState> units)
        {
            if (units == null) return;
            units.Sort((a, b) => b.Revenue.CompareTo(a.Revenue));
        }

        private static AdUnitState FindUnit(List<AdUnitState> units, string adUnitId)
        {
            if (units == null) return null;
            foreach (var unit in units)
                if (unit.AdUnitId == adUnitId) return unit;
            return null;
        }

        // Returns the unit matching adUnitId, or the first unit when adUnitId is empty (LevelPlay compatibility).
        private static AdUnitState FindUnitOrFirst(List<AdUnitState> units, string adUnitId)
        {
            if (units == null || units.Count == 0) return null;
            if (string.IsNullOrEmpty(adUnitId)) return units[0];
            return FindUnit(units, adUnitId);
        }

        // Returns the best ready unit based on IsMultipleAdUnitsEnabled:
        // - false: only the primary (originally index-0) unit is eligible
        // - true:  highest-revenue ready unit (list is sorted descending by revenue)
        private AdUnitState GetEligibleUnit(List<AdUnitState> units)
        {
            if (units == null || units.Count == 0) return null;
            if (!IsMultipleAdUnitsEnabled)
            {
                foreach (var u in units)
                    if (u.IsPrimary) return u.IsReady ? u : null;
                return null;
            }
            return GetBestReadyUnit(units);
        }

        private static AdUnitState GetBestReadyUnit(List<AdUnitState> units)
        {
            if (units == null) return null;
            // Units are kept sorted by revenue descending, so the first ready unit is the highest-revenue one.
            foreach (var unit in units)
                if (unit.IsReady) return unit;
            return null;
        }

        private class AdUnitState
        {
            public string AdUnitId;
            public double Revenue;
            public bool IsReady;
            public bool IsPrimary;
            public float TimeBeforeLoad;
            public int MaxReloadAttempts;
            public float ReloadDelay;
            public bool IsDelayConstant;
            public int ReloadAfterFailAttempts;

            public float GetConstantTimeBeforeReload() => ReloadDelay;
            public float GetIncreasingTimeBeforeReload() => Mathf.Min(2 * ReloadAfterFailAttempts, ReloadDelay);
        }

        // -------------------------------------------------------------------------------------------------------------

        public void RegisterAdCallback(AdType adType, AdCallbackType eventType, Action callback)
        {
            switch (adType)
            {
                case AdType.AppOpen:
                    switch (eventType)
                    {
                        case AdCallbackType.AdLoaded:
                            _appOpenOnAdLoadedEvent += callback;
                            break;
                        case AdCallbackType.AdLoadFailed:
                            _appOpenOnAdLoadFailedEvent += callback;
                            break;
                        case AdCallbackType.AdDisplayed:
                            _appOpenOnAdDisplayedEvent += callback;
                            break;
                        case AdCallbackType.AdDisplayFailed:
                            _appOpenOnAdDisplayFailedEvent += callback;
                            break;
                        case AdCallbackType.AdClosed:
                            _appOpenOnAdClosedEvent += callback;
                            break;
                    }

                    break;

                case AdType.Banner:
                    switch (eventType)
                    {
                        case AdCallbackType.AdLoaded:
                            _bannerOnAdLoadedEvent += callback;
                            break;
                        case AdCallbackType.AdLoadFailed:
                            _bannerOnAdFailedToLoadEvent += callback;
                            break;
                        case AdCallbackType.AdDisplayed:
                            _bannerOnAdDisplayedEvent += callback;
                            break;
                        case AdCallbackType.AdDisplayFailed:
                            _bannerOnAdDisplayFailedEvent += callback;
                            break;
                        case AdCallbackType.AdClosed:
                            _bannerOnAdClosedEvent += callback;
                            break;
                    }

                    break;

                case AdType.Interstitial:
                    switch (eventType)
                    {
                        case AdCallbackType.AdLoaded:
                            _interstitialOnAdLoadedEvent += callback;
                            break;
                        case AdCallbackType.AdLoadFailed:
                            _interstitialOnAdFailedToLoadEvent += callback;
                            break;
                        case AdCallbackType.AdDisplayed:
                            _interstitialOnAdDisplayedEvent += callback;
                            break;
                        case AdCallbackType.AdDisplayFailed:
                            _interstitialOnAdDisplayFailedEvent += callback;
                            break;
                        case AdCallbackType.AdClosed:
                            _interstitialOnAdClosedEvent += callback;
                            break;
                    }

                    break;

                case AdType.RewardedVideo:
                    switch (eventType)
                    {
                        case AdCallbackType.AdLoaded:
                            _rewardedAdLoadedEvent += callback;
                            break;
                        case AdCallbackType.AdLoadFailed:
                            _rewardedAdFailedToLoadEvent += callback;
                            break;
                        case AdCallbackType.AdDisplayed:
                            _rewardedAdDisplayedEvent += callback;
                            break;
                        case AdCallbackType.AdDisplayFailed:
                            _rewardedAdDisplayFailedEvent += callback;
                            break;
                        case AdCallbackType.AdClosed:
                            _rewardedAdClosedEvent += callback;
                            break;
                    }

                    break;
            }
        }

        public void RegisterAdRevenuePaidEvent(AdType adType, Action<AdImpressionData> callback)
        {
#if UNITY_WEBGL
            return;
#endif
            if (_adService == null)
            {
                Utils.MocaLibLogError(TAG, "RegisterAdRevenuePaidEvent called before AdManager is initialized. Call Initialize first.");
                return;
            }

            switch (adType)
            {
                case AdType.AppOpen:
                    _adService.RegisterAppOpenRevenuePaidEvent(callback);
                    break;
                case AdType.Banner:
                    _adService.RegisterBannerRevenuePaidEvent(callback);
                    break;
                case AdType.Interstitial:
                    _adService.RegisterInterstitialRevenuePaidEvent(callback);
                    break;
                case AdType.RewardedVideo:
                    _adService.RegisterRewardedRevenuePaidEvent(callback);
                    break;
                default:
                    _adService.RegisterAppOpenRevenuePaidEvent(callback);
                    _adService.RegisterBannerRevenuePaidEvent(callback);
                    _adService.RegisterInterstitialRevenuePaidEvent(callback);
                    _adService.RegisterRewardedRevenuePaidEvent(callback);
                    break;
            }
        }

        // -------------------------------------------------------------------------------------------------------------

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                _appOpenAdCooldownTime = DateTime.Now;
            }

            // [ANR-PATCH] removed auto hide/re-show banner on pause (ANR source, no longer needed).
        }
    }
}
