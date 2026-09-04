#if MOCALIB_AD_PROVIDER_LEVELPLAY

using System;
using System.Collections.Generic;
using UnityEngine;

using Titipi.MocaLib.Runtime.Common;
using Titipi.MocaLib.Runtime.Services.Internal;

namespace Titipi.MocaLib.Runtime.Services
{
    public class LevelPlayAdService : AdService
    {
        private const string TAG = "LevelPlayAdService";

        public override void Initialize(string appKey, bool appOpen, bool banner, bool interstitial, bool rewarded, string userId, Action onInitializedEvent)
        {
            Utils.MocaLibLog(TAG, "Initialize");

            IronSource.Agent.setMetaData("is_test_suite", "enable");

            IronSourceEvents.onSdkInitializationCompletedEvent += () =>
            {
                onInitializedEvent?.Invoke();
            };

            if (OnAdImpressionEvent != null)
            {
                IronSourceEvents.onImpressionDataReadyEvent += AdImpressionEvent;
            }

            var adUnits = new List<string>();
            if (banner) adUnits.Add(IronSourceAdUnits.BANNER);
            if (interstitial) adUnits.Add(IronSourceAdUnits.INTERSTITIAL);
            if (rewarded) adUnits.Add(IronSourceAdUnits.REWARDED_VIDEO);

            if (!string.IsNullOrEmpty(userId))
                IronSource.Agent.setUserId(userId);
            IronSource.Agent.setManualLoadRewardedVideo(true);
            IronSource.Agent.init(appKey, adUnits.ToArray());
        }

        private void AdImpressionEvent(IronSourceImpressionData impressionData)
        {
            var adImpressionData = new AdImpressionData
            {
                AdPlatform = "LevelPlay",
                AdSource = impressionData.adNetwork,
                AdUnitName = impressionData.mediationAdUnitName,
                AdFormat = impressionData.adFormat,
                Value = impressionData.revenue ?? 0,
                Currency = "USD",
                CreativeId = impressionData.creativeId,
                InstanceId = impressionData.instanceId,
                Precision = impressionData.precision,
            };

            OnAdImpressionEvent.Invoke(adImpressionData);
        }

        public override void InitializeAppOpenAdCallbacks()
        {
            throw new NotImplementedException();
        }

        public override void InitializeBannerAdCallbacks()
        {
            // IronSourceBannerEvents.onAdLoadedEvent += adInfo => { };
            // IronSourceBannerEvents.onAdLoadFailedEvent += error => { };
            // IronSourceBannerEvents.onAdClickedEvent += adInfo => { };
            // IronSourceBannerEvents.onAdScreenPresentedEvent += adInfo => { };
            // IronSourceBannerEvents.onAdScreenDismissedEvent += adInfo => { };
            // IronSourceBannerEvents.onAdLeftApplicationEvent += adInfo => { };
        }

        public override void InitializeInterstitialAdCallbacks()
        {
            IronSourceInterstitialEvents.onAdReadyEvent += adInfo =>
            {
                InterstitialOnAdLoadedEvent?.Invoke("", 0);
            };

            IronSourceInterstitialEvents.onAdLoadFailedEvent += error =>
            {
                InterstitialOnAdLoadFailedEvent?.Invoke("");
            };

            IronSourceInterstitialEvents.onAdOpenedEvent += adInfo =>
            {
                InterstitialOnAdDisplayedEvent?.Invoke("");
            };

            IronSourceInterstitialEvents.onAdShowSucceededEvent += adInfo => { };

            IronSourceInterstitialEvents.onAdShowFailedEvent += (error, adInfo) =>
            {
                InterstitialOnAdDisplayFailedEvent?.Invoke("");
            };

            IronSourceInterstitialEvents.onAdClickedEvent += adInfo => { };

            IronSourceInterstitialEvents.onAdClosedEvent += adInfo =>
            {
                InterstitialOnAdClosedEvent?.Invoke("");
            };
        }

        public override void InitializeRewardedAdCallbacks()
        {
            IronSourceRewardedVideoEvents.onAdAvailableEvent += adInfo =>
            {
                RewardedOnAdLoadedEvent?.Invoke("", 0);
            };

            IronSourceRewardedVideoEvents.onAdUnavailableEvent += () =>
            {
                RewardedOnAdLoadFailedEvent?.Invoke("");
            };

            IronSourceRewardedVideoEvents.onAdOpenedEvent += adInfo =>
            {
                RewardedOnAdDisplayedEvent?.Invoke("");
            };

            IronSourceRewardedVideoEvents.onAdShowFailedEvent += (error, adInfo) =>
            {
                RewardedOnAdDisplayFailedEvent?.Invoke("");
            };

            IronSourceRewardedVideoEvents.onAdRewardedEvent += (placement, adInfo) =>
            {
                RewardedOnAdReceivedRewardEvent?.Invoke();
            };

            IronSourceRewardedVideoEvents.onAdClickedEvent += (placement, adInfo) => { };

            IronSourceRewardedVideoEvents.onAdClosedEvent += adInfo =>
            {
                RewardedOnAdClosedEvent?.Invoke("");
            };
        }

        public override void LoadAppOpenAd(string adUnitId = "")
        {
            throw new NotImplementedException();
        }

        public override void LoadBannerAd(string adUnitId = "")
        {
            IronSource.Agent.loadBanner(IronSourceBannerSize.SMART, IronSourceBannerPosition.BOTTOM);
        }

        public override void LoadInterstitialAd(string adUnitId = "")
        {
            IronSource.Agent.loadInterstitial();
        }

        public override void LoadRewardedAd(string adUnitId = "")
        {
            IronSource.Agent.loadRewardedVideo();
        }

        public override void ShowAppOpenAd(string adUnitId = "")
        {
            throw new NotImplementedException();
        }

        public override void ShowBannerAd(string adUnitId = "")
        {
            IronSource.Agent.displayBanner();
        }

        public override void HideBannerAd(string adUnitId = "")
        {
            IronSource.Agent.hideBanner();
        }

        public override void ShowInterstitialAd(string adUnitId = "")
        {
            IronSource.Agent.showInterstitial();
        }

        public override void ShowRewardedAd(string adUnitId = "")
        {
            IronSource.Agent.showRewardedVideo();
        }

        public override float GetBannerHeight(string adUnitId = "")
        {
#if UNITY_EDITOR
            return 200;
#endif

            float height = IronSource.Agent.getMaximalAdaptiveHeight(320);

            float heightInPixels = height * Mathf.RoundToInt(Screen.dpi / 160);

#if UNITY_IOS
            heightInPixels += (Screen.height - (Screen.safeArea.height + Screen.safeArea.y));
#endif

            return heightInPixels;
        }

        public override bool IsAppOpenAdReady(string adUnitId = "")
        {
            throw new NotImplementedException();
        }

        public override bool IsInterstitialAdReady(string adUnitId = "")
        {
            return IronSource.Agent.isInterstitialReady();
        }

        public override bool IsRewardedAdReady(string adUnitId = "")
        {
            return IronSource.Agent.isRewardedVideoAvailable();
        }

        public override void ShowIntegrationDebugger()
        {
            IronSource.Agent.launchTestSuite();
        }

        private void OnApplicationPause(bool isPaused)
        {
            IronSource.Agent.onApplicationPause(isPaused);
        }
    }
}

#endif // MOCALIB_AD_PROVIDER_LEVELPLAY
