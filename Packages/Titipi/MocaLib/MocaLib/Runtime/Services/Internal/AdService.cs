using System;

namespace Titipi.MocaLib.Runtime.Services.Internal
{
    public enum AdType
    {
        None = 0,
        AppOpen = 1,
        Banner = 2,
        Interstitial = 3,
        RewardedVideo = 4
    }

    public struct AdImpressionData
    {
        public string AdPlatform;
        public string AdSource;
        public string AdUnitId;
        public string AdFormat;
        public double Value;
        public string Currency;
        public string CreativeId;
        public string InstanceId;
        public string Precision;
    }

    public abstract class AdService
    {
        public Action AppOpenOnAdLoadedEvent;
        public Action AppOpenOnAdLoadFailedEvent;
        public Action AppOpenOnAdDisplayedEvent;
        public Action AppOpenOnAdDisplayFailedEvent;
        public Action AppOpenOnAdClosedEvent;

        public Action BannerOnAdLoadedEvent;
        public Action BannerOnAdLoadFailedEvent;
        public Action BannerOnAdDisplayedEvent;
        public Action BannerOnAdDisplayFailedEvent;
        public Action BannerOnAdClosedEvent;

        public Action<string, double> InterstitialOnAdLoadedEvent;
        public Action<string> InterstitialOnAdLoadFailedEvent;
        public Action<string> InterstitialOnAdDisplayedEvent;
        public Action<string> InterstitialOnAdDisplayFailedEvent;
        public Action<string> InterstitialOnAdClosedEvent;

        public Action<string, double> RewardedOnAdLoadedEvent;
        public Action<string> RewardedOnAdLoadFailedEvent;
        public Action<string> RewardedOnAdDisplayedEvent;
        public Action<string> RewardedOnAdDisplayFailedEvent;
        public Action<string> RewardedOnAdClosedEvent;
        public Action RewardedOnAdReceivedRewardEvent;

        public Action<AdImpressionData> AppOpenOnAdImpressionEvent;
        public Action<AdImpressionData> BannerOnAdImpressionEvent;
        public Action<AdImpressionData> InterstitialOnAdImpressionEvent;
        public Action<AdImpressionData> RewardedOnAdImpressionEvent;

        public abstract void Initialize(string appKey, bool appOpen, bool banner, bool interstitial, bool rewarded, string userId, Action onSdkInitializedEvent);

        public abstract void InitializeAppOpenAdCallbacks();
        public abstract void InitializeBannerAdCallbacks();
        public abstract void InitializeInterstitialAdCallbacks();
        public abstract void InitializeRewardedAdCallbacks();

        public abstract void LoadAppOpenAd(string adUnitId);
        public abstract void LoadBannerAd(string adUnitId);
        public abstract void LoadInterstitialAd(string adUnitId);
        public abstract void LoadRewardedAd(string adUnitId);

        public abstract void ShowAppOpenAd(string adUnitId);
        public abstract void ShowBannerAd(string adUnitId);
        public abstract void HideBannerAd(string adUnitId);
        public abstract void ShowInterstitialAd(string adUnitId);
        public abstract void ShowRewardedAd(string adUnitId);

        public abstract float GetBannerHeight();

        public abstract bool IsAppOpenAdReady(string adUnitId);
        public abstract bool IsInterstitialAdReady(string adUnitId);
        public abstract bool IsRewardedAdReady(string adUnitId);

        public abstract void RegisterAppOpenRevenuePaidEvent(Action<AdImpressionData> callback);
        public abstract void RegisterBannerRevenuePaidEvent(Action<AdImpressionData> callback);
        public abstract void RegisterInterstitialRevenuePaidEvent(Action<AdImpressionData> callback);
        public abstract void RegisterRewardedRevenuePaidEvent(Action<AdImpressionData> callback);

        public abstract void DisableB2B();
        public abstract void ShowIntegrationDebugger();
    }
}
