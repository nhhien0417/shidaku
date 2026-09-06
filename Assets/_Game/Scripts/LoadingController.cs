using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.Common;
using _Game.Scripts.LiveOps;
using _Game.Scripts.Utils;
using Titipi.MocaLib.Runtime.Services;
using Titipi.MocaLib.Runtime.Startup;
using Titipi.MocaLib.Runtime.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UserDataPack;
using Analytics.Providers;
using CostCenter;
using Design;
using CostCenter.Attribution;
using Design.DataHolder;
using Design.Ids;
using Design.Structures;
using Game.InappMessageHandlers;
using MEC;
using Game.PromotionOffer;
using Game.Utils;
using RemoteConfigs;
using UnityEngine.Purchasing;

public class LoadingController : BaseLoadingController
{
    [SerializeField] protected float _minLoadingTime = 1.5f;

    protected static float _flexMaxLoadingTime = 7f;
    protected List<Item> _pendingPurchaseItems = new();

    protected override IEnumerator StartLoadingScreen()
    {
        TotalPlaytimeTracker.Initialize();

        UpdateLoadingBarProgress(_loadingTime);
        yield return GameLocalization.InitializeRoutine(); // IMPORTANT: do not place this line after op.allowSceneActivation = false

        var targetScene = Key.MAIN_MENU_SCENE;
        AsyncOperation op = SceneManager.LoadSceneAsync(targetScene);
        op.allowSceneActivation = false;

        var isAppOpenAdShown = false;
        var step = 0;
        _flexMaxLoadingTime = _maxLoadingTime;

#if !UNITY_EDITOR
        DebugLogHelper.PauseLogging(_maxLoadingTime, false, () => Debug.unityLogger.logEnabled = false);
#endif

        while (!_loadingDone)
        {
            _loadingTime += Time.deltaTime;

            switch (step)
            {
                // Allow some "warm-up"
                case 0:
                case 1:
                case 2:
                    step++;
                    break;

                case 3:
#if UNITY_ANDROID
                    _versionText.text = $"Version: {GameVersionInfo.BUILD_VERSION}";
#elif UNITY_IOS
                    _versionText.text = $"Version: {GameVersionInfo.BUILD_VERSION} ({GameVersionInfo.BUILD_NUMBER})";
#endif

                    step++;
                    break;

                case 4:
                    MocaLib.Instance.RegisterAdEventsHandler(AdsManager.Initialize);
                    MocaLib.Instance.Initialize(
                        onFirebaseInitialized: () =>
                        {
                            CCFirebase.instance.OnInitialized(true);
                            CCAttribution.instance.TrackingAttribution(null);
                            RemoteConfigHelper.Instance?.Initialize(OnRemoteConfigFetchCompleted);

                            FIAMManager.Instance.RegisterPopupHandler(new FIAMPopupHandler());
                            FIAMManager.Instance.RegisterBannerMessageReceivedEvent(HandleReceivedBannerMessage);

                            UserProperty.Initialize();
                            if (MocaLib.Instance.IsFirstOpen)
                            {
                                UserProperty.SetApproximateDeviceId(SystemInfo.deviceUniqueIdentifier);
                            }
                        },
                        onPlayerProfileLoaded: (success, error) =>
                        {
                            if (success)
                            {
                                UserProfileHelper.SyncUserProfile();
                                UserData.Instance.UserProfile.SubmitLeaderboardScore();
                            }
                            else
                            {
                                Debug.LogError($"Failed to load player profile: {error}");
                            }
                        }
                    );

                    var initialIapProducts = DesignDataHolder.Instance?.GetAllIapProductIds();
                    MocaLib.Instance.IAPManager.OnRestorePurchases += OnRestorePurchase;
                    MocaLib.Instance.IAPManager.Initialize(initialIapProducts, () =>
                    {
                        Iap.IapCatalogSync.MarkIapInitialized(initialIapProducts);
                        Iap.IapCatalogSync.SyncNewProducts(DesignDataHolder.Instance?.GetAllIapProductIds());

#if UNITY_ANDROID
                        var purchasedProducts = MocaLib.Instance.IAPManager.GetAllPurchasedNonConsumables();
                        DesignDataHolder.Instance?.RestorePurchasedNonConsumableItems(purchasedProducts);
#endif
                    });

                    while (!MocaLib.Instance.FirebaseService.IsInitialized && _loadingTime < _maxLoadingTime)
                    {
                        _loadingTime += Time.deltaTime;
                        UpdateLoadingBarProgress(_loadingTime);
                        yield return null;
                    }

                    while (!MocaLib.Instance.AdManager.IsInitialized && _loadingTime < _maxLoadingTime)
                    {
                        _loadingTime += Time.deltaTime;
                        UpdateLoadingBarProgress(_loadingTime);
                        yield return null;
                    }

                    step++;
                    break;

                case 5:
                    Analytics.AnalyticsManager.Instance.Initialize(new MocaLibAnalyticsProvider());

                    UserData.Instance.NewVersionMigrateData.Migrate();
                    UserData.Instance.Save();

                    step++;
                    break;

                case 6:
                    while (!DateTimeManager.IsUpToDate && _loadingTime < _maxLoadingTime)
                    {
                        _loadingTime += Time.deltaTime;
                        UpdateLoadingBarProgress(_loadingTime);
                        yield return null;
                    }

                    if (_loadingTime < _minLoadingTime)
                    {
                        _flexMaxLoadingTime = _minLoadingTime;
                        while (_loadingTime < _minLoadingTime)
                        {
                            _loadingTime += Time.deltaTime;
                            UpdateLoadingBarProgress(_loadingTime);
                            yield return null;
                        }
                    }
                    else
                    {
                        _loadingTime = _maxLoadingTime;
                    }
                    _loadingDone = true;

                    Timing.RunCoroutine(ShowBanner());
                    break;
            }

            UpdateLoadingBarProgress(_loadingTime);

            yield return new WaitForEndOfFrame();
        }

        if (_pendingPurchaseItems.Count > 0)
        {
            UIManager.Instance?.ShowUIGroupOverlay<UIRewards>(new UIRewards.Data()
            {
                Rewards = _pendingPurchaseItems
            });
        }

        op.allowSceneActivation = true;
    }

    protected override void UpdateLoadingBarProgress(float loadingTime)
    {
        if (loadingTime > _flexMaxLoadingTime) loadingTime = _flexMaxLoadingTime;
        var progress = loadingTime / _flexMaxLoadingTime;
        _loadingBar.value = progress;
    }

    protected override void StartSplashScreen()
    {
        _splashScreen.SetActive(false);
        _loadingScreen.SetActive(true);
        StartCoroutine(nameof(StartLoadingScreen));
    }

    private void OnRemoteConfigFetchCompleted(bool success)
    {
        var remoteConfigMng = RemoteConfigHelper.Instance;
        if (remoteConfigMng == null)
            return;

        if (!success)
        {
            remoteConfigMng.ApplyRemoteConfigs();
            return;
        }

        var cheatEnabled = remoteConfigMng.GetConfig(RemoteConfigKey.CHEATS_ENABLED, false);
#if UNITY_EDITOR
        cheatEnabled = true;
#endif
        DateTimeManager.CheatEnabled = cheatEnabled;

#if !UNITY_EDITOR
            var enableDebugLog = remoteConfigMng.GetConfig(RemoteConfigKey.ENABLE_DEBUG_LOGS, false);
            DebugLogHelper.ResumeLogging(enableDebugLog);
            if (enableDebugLog != Debug.unityLogger.logEnabled)
            {
                Debug.unityLogger.logEnabled = enableDebugLog;
            }
#endif

        remoteConfigMng.ApplyRemoteConfigs();

#if UNITY_IOS
            var enableIosSubmitPreview = remoteConfigMng.GetConfig(RemoteConfigKey.ENABLE_IOS_SUBMIT_PREVIEW, false);
            if (enableIosSubmitPreview)
                EnableIosSubmitPreview();
#endif
    }

    private void HandleReceivedBannerMessage(BannerMessageData messageData)
    {
        PromotionOfferManager.Instance.HandleReceivedBannerMessage(messageData);
        OneCTAPopupHandler.HandleReceivedBannerMessage(messageData);
    }

    private IEnumerator<float> ShowBanner()
    {
        while (!MocaLib.Instance.AdManager.IsInitialized)
        {
            yield return Timing.WaitForOneFrame;
        }

        AdsManager.ShowBanner(true);
    }

    private void OnRestorePurchase(Product product, bool isPendingPurchase)
    {
        // this.Log($"Restoring purchase for product {product.definition.id}, isPendingPurchase: {isPendingPurchase}");

        if (isPendingPurchase)
        {
            var items = DesignDataHolder.Instance?.ProcessPendingPurchase(product);
            if (items is { Count: > 0 })
                _pendingPurchaseItems.AddRange(items);
        }
#if UNITY_IOS
        else
        {
            DesignDataHolder.Instance?.RestorePurchasedNonConsumableItems(new List<Product> { product });
        }
#endif
    }

#if UNITY_IOS
    private void EnableIosSubmitPreview()
    {
        var userData = UserData.Instance;
        if (userData.GetNonconsumableItemAmount(ItemId.NoAds) <= 0)
        {
            PromotionOfferManager.Instance.HandleReceivedBannerMessage(new()
            {
                CustomData = new Dictionary<string, string>()
                {
                    { InAppMessageDataKey.Action, MessageActionKey.ActiveOffer },
                    { InAppMessageDataKey.OfferId, PredefinedOfferId.NoAdsDiscount },
                    { InAppMessageDataKey.Priority, "1" },
                    { InAppMessageDataKey.DurationInHours, "12" },
                    { InAppMessageDataKey.CustomData, "" },
                }
            });
        }

        PromotionOfferManager.Instance.HandleReceivedBannerMessage(new()
        {
            CustomData = new Dictionary<string, string>()
            {
                { InAppMessageDataKey.Action, MessageActionKey.ActiveOffer },
                { InAppMessageDataKey.OfferId, PredefinedOfferId.StarterPack },
                { InAppMessageDataKey.Priority, "0" },
                { InAppMessageDataKey.DurationInHours, "12" },
                { InAppMessageDataKey.CustomData, "" },
            }
        });

        PromotionOfferManager.Instance.ActiveAllOffersSilently();

        var designData = DesignDataHolder.Instance;
        var artpuzzleUnlockConfig = designData.FeatureUnlockData.UnlockConfigs.Find(x => x.FeatureType == FeatureType.ArtPuzzle);
        if (artpuzzleUnlockConfig != null)
            artpuzzleUnlockConfig.UnlockLevel = 0;
    }
#endif
}
