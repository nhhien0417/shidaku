#if MOCALIB_AD_PROVIDER_APPLOVIN

using System;
using UnityEngine;

using Titipi.MocaLib.Runtime.Common;
using Titipi.MocaLib.Runtime.Services.Internal;

namespace Titipi.MocaLib.Runtime.Services
{
    public class AppLovinAdService : AdService
    {
        private const string TAG = "AppLovinAdService";

        public override void Initialize(string appKey, bool appOpen, bool banner, bool interstitial, bool rewarded, string userId, Action onInitializedEvent)
        {
            Utils.MocaLibLog(TAG, "Initialize");

            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                onInitializedEvent?.Invoke();
            };

            if (!string.IsNullOrEmpty(userId))
            {
                MaxSdk.SetUserId(userId);
                Utils.MocaLibLog(TAG, $"Init service with user id: {userId}");
            }

            MaxSdk.InitializeSdk();
        }

        private AdImpressionData CreateAdImpressionData(MaxSdkBase.AdInfo adInfo)
        {
            var adImpressionData = new AdImpressionData
            {
                AdPlatform = "AppLovin",
                AdSource = adInfo.NetworkName,
                AdUnitId = adInfo.AdUnitIdentifier,
                AdFormat = adInfo.AdFormat,
                Value = adInfo.Revenue,
                Currency = "USD",
                CreativeId = adInfo.CreativeIdentifier,
                InstanceId = "",
                Precision = adInfo.RevenuePrecision,
            };

            return adImpressionData;
        }

        private void AppOpenRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            var adImpressionData = CreateAdImpressionData(adInfo);

            MocaLib.Instance.MMPManager.LogAdRevenue(adImpressionData);
            AppOpenOnAdImpressionEvent?.Invoke(adImpressionData);
        }

        private void BannerRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            var adImpressionData = CreateAdImpressionData(adInfo);

            MocaLib.Instance.MMPManager.LogAdRevenue(adImpressionData);
            BannerOnAdImpressionEvent?.Invoke(adImpressionData);
        }

        private void InterstitialRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            var adImpressionData = CreateAdImpressionData(adInfo);

            MocaLib.Instance.MMPManager.LogAdRevenue(adImpressionData);
            InterstitialOnAdImpressionEvent?.Invoke(adImpressionData);
        }

        private void RewardedRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            var adImpressionData = CreateAdImpressionData(adInfo);

            MocaLib.Instance.MMPManager.LogAdRevenue(adImpressionData);
            RewardedOnAdImpressionEvent?.Invoke(adImpressionData);
        }

        public override void InitializeAppOpenAdCallbacks()
        {
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += (s, info) =>
            {
                AppOpenOnAdLoadedEvent?.Invoke();
            };

            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += (s, info) =>
            {
                AppOpenOnAdLoadFailedEvent?.Invoke();
            };

            MaxSdkCallbacks.AppOpen.OnAdClickedEvent += (s, info) => { };

            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += (s, info) =>
            {
                AppOpenOnAdDisplayedEvent?.Invoke();
            };

            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += (s, err, info) =>
            {
                AppOpenOnAdDisplayFailedEvent?.Invoke();
            };

            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += (s, info) =>
            {
                AppOpenOnAdClosedEvent?.Invoke();
            };

            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += AppOpenRevenuePaidEvent;
        }

        public override void InitializeBannerAdCallbacks()
        {
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += (s, info) =>
            {
                BannerOnAdLoadedEvent?.Invoke();
            };

            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += (s, info) =>
            {
                BannerOnAdLoadFailedEvent?.Invoke();
            };

            MaxSdkCallbacks.Banner.OnAdClickedEvent += (s, info) => { };

            MaxSdkCallbacks.Banner.OnAdExpandedEvent += (s, info) => { };

            MaxSdkCallbacks.Banner.OnAdCollapsedEvent += (s, info) => { };

            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += BannerRevenuePaidEvent;
        }

        public override void InitializeInterstitialAdCallbacks()
        {
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += (s, info) =>
            {
                InterstitialOnAdLoadedEvent?.Invoke(s, info.Revenue);
            };

            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += (s, info) =>
            {
                InterstitialOnAdLoadFailedEvent?.Invoke(s);
            };

            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += (s, info) =>
            {
                InterstitialOnAdDisplayedEvent?.Invoke(s);
            };

            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += (s, err, info) =>
            {
                InterstitialOnAdDisplayFailedEvent?.Invoke(s);
            };

            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += (s, info) => { };

            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += (s, info) =>
            {
                InterstitialOnAdClosedEvent?.Invoke(s);
            };

            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += InterstitialRevenuePaidEvent;
        }

        public override void InitializeRewardedAdCallbacks()
        {
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += (s, info) =>
            {
                RewardedOnAdLoadedEvent?.Invoke(s, info.Revenue);
            };

            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += (s, info) =>
            {
                RewardedOnAdLoadFailedEvent?.Invoke(s);
            };

            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += (s, info) =>
            {
                RewardedOnAdDisplayedEvent?.Invoke(s);
            };

            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += (s, err, info) =>
            {
                RewardedOnAdDisplayFailedEvent?.Invoke(s);
            };

            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += (s, reward, info) =>
            {
                RewardedOnAdReceivedRewardEvent?.Invoke();
            };

            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += (s, info) => { };

            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += (s, info) =>
            {
                RewardedOnAdClosedEvent?.Invoke(s);
            };

            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += RewardedRevenuePaidEvent;
        }

        public override void LoadAppOpenAd(string adUnitId)
        {
            if (string.IsNullOrEmpty(adUnitId)) return;
            
            MaxSdk.LoadAppOpenAd(adUnitId);
        }

        public override void LoadBannerAd(string adUnitId)
        {
            if (string.IsNullOrEmpty(adUnitId)) return;

            MaxSdk.CreateBanner(adUnitId, MaxSdkBase.BannerPosition.BottomCenter);
            MaxSdk.SetBannerBackgroundColor(adUnitId, new Color(1, 1, 1, 0));
        }

        public override void LoadInterstitialAd(string adUnitId)
        {
            if (string.IsNullOrEmpty(adUnitId)) return;

            MaxSdk.LoadInterstitial(adUnitId);
        }

        public override void LoadRewardedAd(string adUnitId)
        {
            if (string.IsNullOrEmpty(adUnitId)) return;

            MaxSdk.LoadRewardedAd(adUnitId);
        }

        public override void ShowAppOpenAd(string adUnitId)
        {
            if (string.IsNullOrEmpty(adUnitId)) return;

            MaxSdk.ShowAppOpenAd(adUnitId);
        }

        public override void ShowBannerAd(string adUnitId)
        {
            if (string.IsNullOrEmpty(adUnitId)) return;

            MaxSdk.ShowBanner(adUnitId);
        }

        public override void HideBannerAd(string adUnitId)
        {
            if (string.IsNullOrEmpty(adUnitId)) return;

            MaxSdk.HideBanner(adUnitId);
        }

        public override void ShowInterstitialAd(string adUnitId)
        {
            if (string.IsNullOrEmpty(adUnitId)) return;

            MaxSdk.ShowInterstitial(adUnitId);
        }

        public override void ShowRewardedAd(string adUnitId)
        {
            if (string.IsNullOrEmpty(adUnitId)) return;

            MaxSdk.ShowRewardedAd(adUnitId);
        }

        public override float GetBannerHeight()
        {
#if UNITY_EDITOR
            return 168; // this is the height of the `BannerBottom(Clone)/Panel` game object as seen in the Hierarchy view.

            // For a generic solution, we can do something like this:

            // var banner = GameObject.Find("BannerBottom(Clone)/Panel");
            // if (banner == null) return 200;
            // return banner.gameObject.GetComponent<RectTransform>().rect.height;
#endif

            var heightDp = MaxSdkUtils.GetAdaptiveBannerHeight();
            var density = MaxSdkUtils.GetScreenDensity();

            return heightDp * density;
        }

        public override bool IsAppOpenAdReady(string adUnitId)
        {
            if (string.IsNullOrEmpty(adUnitId)) return false;

            return MaxSdk.IsAppOpenAdReady(adUnitId);
        }

        public override bool IsInterstitialAdReady(string adUnitId)
        {
            if (string.IsNullOrEmpty(adUnitId)) return false;

            return MaxSdk.IsInterstitialReady(adUnitId);
        }

        public override bool IsRewardedAdReady(string adUnitId)
        {
            if (string.IsNullOrEmpty(adUnitId)) return false;

            return MaxSdk.IsRewardedAdReady(adUnitId);
        }

        public override void RegisterAppOpenRevenuePaidEvent(Action<AdImpressionData> callback)
        {
            AppOpenOnAdImpressionEvent += callback;
        }

        public override void RegisterBannerRevenuePaidEvent(Action<AdImpressionData> callback)
        {
            BannerOnAdImpressionEvent += callback;
        }

        public override void RegisterInterstitialRevenuePaidEvent(Action<AdImpressionData> callback)
        {
            InterstitialOnAdImpressionEvent += callback;
        }

        public override void RegisterRewardedRevenuePaidEvent(Action<AdImpressionData> callback)
        {
            RewardedOnAdImpressionEvent += callback;
        }

        public override void DisableB2B()
        {
            MaxSdk.SetDoNotSell(true);
        }

        public override void ShowIntegrationDebugger()
        {
            MaxSdk.ShowMediationDebugger();
        }
    }
}

#endif // MOCALIB_AD_PROVIDER_APPLOVIN
