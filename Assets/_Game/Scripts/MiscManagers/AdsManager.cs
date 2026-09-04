using System;
using System.Collections.Generic;
using _Game.Scripts.Utils;
using Analytics;
using Design.Structures;
using Titipi.MocaLib.Runtime.Services;
using Titipi.MocaLib.Runtime.Services.Internal;
using UserDataPack;

public class AdsManager
{
    public static int LevelToShowInterstitialAd = 8;
    public static int LevelsEndedToShowInterstitialAd = 1;
    public static int AutoTriggerInterEndgameInterval = 300; // in seconds
    public static int ShowBannerAtLevel = 0;
    public static int AllowMultipleRewardWithRvAtLevel = 0;
    public static int ShowAdBreakAfterXSeconds = 0;
    public static bool ShowInterstitialOnChangeScene = false;

    private static int _gameplayEndCountWithoutIntersAd = LevelsEndedToShowInterstitialAd - 1;
    private static string _lastBannerAdReason = "";
    private static string _lastRewardAdReason = "";
    private static string _lastInterstitialAdReason = "";
    private static string _lastRewardAdPlacement = "";
    private static string _lastInterstitialAdPlacement = "";
    private static string _lastGeneralAdPlacement = Placement.MainGameplay;
    private static float _lastTimeInterDisplay = -300f;

    public static Action OnAdWatched;

    public static float SecondsPassSinceLastInterAd => UnityEngine.Time.realtimeSinceStartup - _lastTimeInterDisplay;

    public static void Initialize()
    {
        MocaLib.Instance.AdManager.RegisterAdCallback(AdType.RewardedVideo, AdCallbackType.AdDisplayed, () =>
        {
            MocaLib.Instance.MMPManager.LogEvent("af_rewarded_displayed");
        });

        MocaLib.Instance.AdManager.RegisterAdCallback(AdType.Interstitial, AdCallbackType.AdDisplayed, () =>
        {
            MocaLib.Instance.MMPManager.LogEvent("af_inters_displayed");
        });

        MocaLib.Instance.AdManager.RegisterAdRevenuePaidEvent(AdType.RewardedVideo, (adRevenueData) =>
        {
            //Debug.Log($"[AdsManager] Ad Revenue Paid: {adRevenueData.AdFormat} {adRevenueData.Value} {adRevenueData.Currency} from {adRevenueData.AdSource} for placement {_lastRewardAdPlacement}");
            Track.Gameplay.OnAdRevenue(adRevenueData.Value);
            MocaLib.Instance.AnalyticsManager.LogAdRevenue(AnalyticsContext.CurrentPlayMode, AnalyticsContext.CurrentLevel, _lastRewardAdPlacement, adRevenueData, GetParametersForAdEvent(adRevenueData, _lastRewardAdReason));
        });

        MocaLib.Instance.AdManager.RegisterAdRevenuePaidEvent(AdType.Interstitial, (adRevenueData) =>
        {
            //Debug.Log($"[AdsManager] Ad Revenue Paid: {adRevenueData.AdFormat} {adRevenueData.Value} {adRevenueData.Currency} from {adRevenueData.AdSource} for placement {_lastRewardAdPlacement}");
            Track.Gameplay.OnAdRevenue(adRevenueData.Value);
            MocaLib.Instance.AnalyticsManager.LogAdRevenue(AnalyticsContext.CurrentPlayMode, AnalyticsContext.CurrentLevel, _lastInterstitialAdPlacement, adRevenueData, GetParametersForAdEvent(adRevenueData, _lastInterstitialAdReason));
        });

        MocaLib.Instance.AdManager.RegisterAdRevenuePaidEvent(AdType.Banner, (adRevenueData) =>
        {
            //Debug.Log($"[AdsManager] Ad Revenue Paid: {adRevenueData.AdFormat} {adRevenueData.Value} {adRevenueData.Currency} from {adRevenueData.AdSource} for placement {_lastRewardAdPlacement}");
            Track.Gameplay.OnAdRevenue(adRevenueData.Value);
            _lastGeneralAdPlacement = Placement.MainGameplay;
            _lastBannerAdReason = "";
            MocaLib.Instance.AnalyticsManager.LogAdRevenue(AnalyticsContext.CurrentPlayMode, AnalyticsContext.CurrentLevel, _lastGeneralAdPlacement, adRevenueData, GetParametersForAdEvent(adRevenueData, _lastBannerAdReason));
        });
    }

    public static void ShowRewardAd(RewardAdAnalyticsData analyticsData, Action onCompleted, Action onFailed)
    {
        _lastRewardAdReason = analyticsData?.Reason ?? "";

        ShowRewardAd(analyticsData?.Placement ?? "", () =>
        {
            if (analyticsData != null)
            {
                Track.Ads.RewardComplete(analyticsData.Placement, analyticsData.RewardName, analyticsData.Rewards, analyticsData.Value);
            }

            onCompleted?.Invoke();

        }, onFailed);
    }

    public static void ShowRewardAd(ShopItem shopItem, Action onCompleted, Action onFailed)
    {
        ShowRewardAd(new RewardAdAnalyticsData(shopItem), onCompleted, onFailed);
    }

    private static void ShowRewardAd(string placement, Action onCompleted, Action onFailed)
    {
        if (!MocaLib.Instance.AdManager.IsInitialized)
        {
            onFailed?.Invoke();
            return;
        }

        _lastRewardAdPlacement = placement;
        _lastGeneralAdPlacement = placement;

        Track.Ads.RewardRequest(placement);
        MocaLib.Instance.AdManager.ShowRewardedAd((success, duration) =>
        {
            if (success)
            {
                Track.Gameplay.OnRvAd(duration);
                OnAdWatched?.Invoke();
                onCompleted?.Invoke();
            }
            else
            {
                onFailed?.Invoke();
            }
        });

        MocaLib.Instance.MMPManager.LogEvent("af_rewarded_logicgame");
    }

    public static bool ShowInterstitialAd(string placement, Action onCompleted)
    {
        if (!MocaLib.Instance.AdManager.IsInitialized)
        {
            onCompleted?.Invoke();
            return false;
        }

        var currentLevel = UserData.Instance.GameplayData.CurrentGameplayLevel;
        if (UserData.Instance.NoAdsActivated() || currentLevel <= LevelToShowInterstitialAd)
        {
            onCompleted?.Invoke();
            return false;
        }
        else
        {
            _lastInterstitialAdPlacement = placement;
            _lastGeneralAdPlacement = placement;
            _lastInterstitialAdReason = "";

            Track.Ads.InterRequest(placement);
            MocaLib.Instance.AdManager.ShowInterstitialAd((success, duration) =>
            {
                if (success)
                {
                    _lastTimeInterDisplay = UnityEngine.Time.realtimeSinceStartup;
                    Track.Gameplay.OnInterAd(duration);
                    Track.Ads.InterShow(placement);
                    OnAdWatched?.Invoke();
                }

                onCompleted?.Invoke();
            });

            MocaLib.Instance.MMPManager.LogEvent("af_inters_logicgame");
            return true;
        }
    }

    public static void ShowIntersAdOnEndgame(bool isWin, Action onCompleted)
    {
        if (!MocaLib.Instance.AdManager.IsInitialized)
        {
            onCompleted?.Invoke();
            return;
        }

        var currentLevel = UserData.Instance.GameplayData.CurrentGameplayLevel;

        if (UserData.Instance.NoAdsActivated() || currentLevel <= LevelToShowInterstitialAd)
        {
            onCompleted?.Invoke();
        }
        else
        {
            _gameplayEndCountWithoutIntersAd += 1;
            if (_gameplayEndCountWithoutIntersAd >= LevelsEndedToShowInterstitialAd)
            {
                _lastInterstitialAdPlacement = Placement.MainGameplay;
                _lastGeneralAdPlacement = Placement.MainGameplay;
                _lastInterstitialAdReason = isWin ? AdReason.IvCompleteLevel : AdReason.IvFailLevel;

                Track.Ads.InterRequest(_lastInterstitialAdPlacement);
                MocaLib.Instance.AdManager.ShowInterstitialAd((success, duration) =>
                {
                    if (success)
                    {
                        _lastTimeInterDisplay = UnityEngine.Time.realtimeSinceStartup;
                        _gameplayEndCountWithoutIntersAd = 0;
                        Track.Gameplay.OnInterAd(duration);
                        OnAdWatched?.Invoke();
                        Track.Ads.InterShow(_lastInterstitialAdPlacement);
                    }

                    onCompleted?.Invoke();
                });

                MocaLib.Instance.MMPManager.LogEvent("af_inters_logicgame");
            }
            else
            {
                onCompleted?.Invoke();
            }
        }
    }

    public static void ShowInterstitialAdWithTimer(bool isWin, Action onCompleted)
    {
        if (UserData.Instance.NoAdsActivated())
        {
            onCompleted?.Invoke();
            return;
        }

        if (UnityEngine.Time.realtimeSinceStartup - _lastTimeInterDisplay >= AutoTriggerInterEndgameInterval)
        {
            ShowIntersAdOnEndgame(isWin, onCompleted);
        }
        else
        {
            onCompleted?.Invoke();
        }
    }

    public static bool InterstitialAdEnabled()
    {
        var userData = UserData.Instance;
        if (userData.NoAdsActivated())
            return false;

        var currentLevel = userData.GameplayData.CurrentGameplayLevel;
        return currentLevel > LevelToShowInterstitialAd;
    }

    public static bool TryShowAdBreak(Action onCompleted)
    {
        if (ShowAdBreakAfterXSeconds <= 0)
            return false;

        if (UserData.Instance.NoAdsActivated())
            return false;

        if (UnityEngine.Time.realtimeSinceStartup - _lastTimeInterDisplay < ShowAdBreakAfterXSeconds)
            return false;

        UIManager.Instance?.ShowLoading(new UILoading.Data()
        {
            Message = "Ad Break...",
            TimeOut = 5
        });

        return ShowInterstitialAd(Placement.MainGameplay, () =>
        {
            UIManager.Instance?.HideLoading();
            onCompleted?.Invoke();
        });
    }

    public static bool InternetRequiredCheckPassed(bool autoShowNotifyPopup = true)
    {
        if ((InternetCheckerWithRemoteConfig.AlwaysRequireInternet || InterstitialAdEnabled()) && InternetCheckerWithRemoteConfig.ShouldShowNoInternetPopup)
        {
            if (autoShowNotifyPopup)
            {
                UIManager.Instance.ShowUIGroupOverlay<UINotify>(new UINotify.Data()
                {
                    Title = "Connection Lost",
                    Text = "Please check your internet connection and try again.",
                });
            }

            return false;
        }

        return true;
    }

    public static void ShowBanner(bool isShow)
    {
        if (!MocaLib.Instance.AdManager.IsInitialized)
            return;

        if (isShow)
        {
            var userData = UserData.Instance;
            if (!userData.NoAdsActivated() && userData.GameplayData.CurrentGameplayLevel >= ShowBannerAtLevel)
                MocaLib.Instance.AdManager.ShowBannerAd();
        }
        else
        {
            MocaLib.Instance.AdManager.HideBannerAd();
        }
    }

    private static Dictionary<string, string> GetParametersForAdEvent(AdImpressionData adRevenueData, string reason)
    {
        return new Dictionary<string, string>
        {
            { "placement", _lastGeneralAdPlacement },
            { "reason", reason },
            { "ad_network", adRevenueData.AdSource ?? "" },
            { "AdUnitIdentifier", adRevenueData.AdUnitId ?? "" },
            { "mediation_type", "MAX" },
            { "level", AnalyticsContext.CurrentLevel.ToString() },
            { "level_mode", AnalyticsContext.CurrentPlayMode },
        };
    }

    public class RewardAdAnalyticsData
    {
        public int CurrentLevel;
        public string ButtonName;
        public string Reason;
        public string RewardName;
        public string Rewards;
        public int Value;
        public string Placement;

        public RewardAdAnalyticsData()
        {

        }

        public RewardAdAnalyticsData(ShopItem shopItem)
        {
            CurrentLevel = UserData.Instance.GameplayData.CurrentGameplayLevel;
            ButtonName = shopItem.ButtonName;
            Reason = shopItem.AdReason;
            RewardName = shopItem.GetAnalyticsItemName();
            Rewards = shopItem.GetAnalyticsRewardIds();
            Value = shopItem.GetAnalyticsValue();
            Placement = shopItem.Placement;
        }
    }
}
