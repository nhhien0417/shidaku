using System;
using UnityEngine;
using System.Threading.Tasks;

#if !UNITY_WEBGL
using Firebase.Analytics;
#endif

#if UNITY_IOS
using System.Collections;
using Unity.Advertisement.IosSupport;
#endif

using Titipi.MocaLib.Runtime.Common;
using Titipi.MocaLib.Runtime.Services.Internal;

namespace Titipi.MocaLib.Runtime.Services
{
    public enum AdProvider
    {
        None,
        [InspectorName("AppLovin MAX")]
        AppLovinMAX,
        [InspectorName("Unity LevelPlay")]
        UnityLevelPlay
    }

    public enum MMPProvider
    {
        None,
        [InspectorName("Adjust")]
        Adjust,
        [InspectorName("AppsFlyer")]
        AppsFlyer
    }

    public sealed class MocaLib : MonoSingleton<MocaLib>
    {
        [Header("“I never dreamed about success. I worked for it.” —Estée Lauder")]

        [Space(4)] [Header("· For development build only ·")]
        [SerializeField] private bool _enableInfoLog;

        [Header("· MMP Manager ·")]
        [SerializeField] private MMPProvider _mmpProvider;
        [SerializeField] private MMPConfig _mmpConfig;
        [ShowIf("_mmpProvider", MMPProvider.AppsFlyer)] [SerializeField] private bool _useAFPurchaseConnector;

        [Space(4)]
        [Header("· Ad Manager ·")]
        [SerializeField] private AdProvider _adProvider;
        [SerializeField] private AdConfig _adConfigAndroid;
        [SerializeField] private AdConfig _adConfigIOS;
        [SerializeField] private bool _useFirebaseAppInstanceIdAsAdUserId;

        [Header("· Rating Manager ·")]
        [SerializeField] private string _iOSAppId;
        [SerializeField] private bool _useInAppRating = true;

        [Header("· Analytics Manager ·")]
        [ReadOnly] [SerializeField] private bool _useFirebase = true;
        [SerializeField] private bool _useByteBrew;
        [SerializeField] private bool _useGameAnalytics;

        [Header("· Remote Config Manager ·")]
        [SerializeField] private bool _useFirebaseRemoteConfig = false;

        [Header("· Push Notification Manager ·")]
        [ReadOnly] [SerializeField] private bool _useFirebasePushNotification = true;

        [Header("· Leaderboard Manager ·")]
        [SerializeField] private bool _useFirebaseLeaderboard;

        [Header("· Firebase App Check ·")]
        [SerializeField] private bool _useFirebaseAppCheck;

        [Header("· Server Time ·")]
        [SerializeField] private bool _useServerTime;

        [Header("· IAP Manager ·")]
        [ReadOnly] [SerializeField] private bool _useIAPManager = true;
        [ShowIf("_useIAPManager", true)] [SerializeField] private IAPConfig _iapConfig;

        [Header("· Facebook Manager ·")]
        [ReadOnly] [SerializeField] private bool _useFacebookManager = true;

        [Header("· Cost Center ·")]
        [SerializeField] private bool _useCostCenter;

        public MMPManager MMPManager { get; private set; }
        public FirebaseService FirebaseService { get; private set; }
        public FIAMManager FIAMManager { get; private set; }
        public AnalyticsManager AnalyticsManager {get; private set;}
        public RatingManager RatingManager {get; private set;}
        public RemoteConfigManager RemoteConfigManager {get; private set;}
        public PushNotificationManager PushNotificationManager {get; private set;}
        public AdManager AdManager {get; private set;}
        public IAPManager IAPManager {get; private set;}
        public FacebookManager FacebookManager {get; private set;}
        public InternetChecker InternetChecker {get; private set;}
        public ServerTimeManager ServerTimeManager {get; private set;}

#if MOCALIB_USE_FIREBASE_LEADERBOARD
        public PlayerProfileManager PlayerProfileManager {get; private set;}
        public LeaderboardManager LeaderboardManager {get; private set;}
#endif

        public Action<bool> OnRemoteConfigFetchCompleted;

        private const string PLAYER_PREFS_KEY_FIRST_OPEN = "moca_first_open";

        public bool IsFirstOpen { get; set; }

        private Action _adEventsHandler;
        private Action _onFirebaseInitialized;

#if MOCALIB_USE_FIREBASE_LEADERBOARD
        private Action<bool, string> _onPlayerProfileLoaded;
#endif

        public void StartInternetChecker(Action<bool> isConnected)
        {
            InternetChecker = gameObject.AddComponent<InternetChecker>();
            InternetChecker.OnInternetStatusChanged = isConnected;
            InternetChecker.Enable();
        }

        // This needs to be called before MocaLib.Initialize()
        public void RegisterAdEventsHandler(Action handler)
        {
            _adEventsHandler = handler;
        }

#if MOCALIB_USE_FIREBASE_LEADERBOARD
        public void Initialize(Action onFirebaseInitialized = null, Action<bool, string> onPlayerProfileLoaded = null)
#else
        public void Initialize(Action onFirebaseInitialized = null)
#endif
        {
#if UNITY_EDITOR
            Debug.Log("🅼🅾🅲🅰🅻🅸🅱 starting...");
#endif

#if UNITY_IOS && !UNITY_EDITOR
            RequestATTracking();
#endif

            _onFirebaseInitialized = onFirebaseInitialized;

#if MOCALIB_USE_FIREBASE_LEADERBOARD
            _onPlayerProfileLoaded = onPlayerProfileLoaded;
#endif

            Instance.OnRemoteConfigFetchCompleted += (success) =>
            {
                if (success)
                {
                    var adConfig = Instance.AdManager.AdConfig;
                    if (adConfig == null)
                    {
                        #if UNITY_ANDROID
                        adConfig = _adConfigAndroid;
                        #elif UNITY_IOS
                        adConfig = _adConfigIOS;
                        #endif
                    }
                }
            };

            InitializeServices();
        }

        public void EnableFirebaseRemoteConfig(bool enable)
        {
            _useFirebaseRemoteConfig = enable;

            // If Firebase is already initialized and RemoteConfigManager is not yet created, create it now
            if (_useFirebaseRemoteConfig
                && FirebaseService != null
                && FirebaseService.IsInitialized
                && RemoteConfigManager == null)
            {
                RemoteConfigManager = gameObject.AddComponent<RemoteConfigManager>();
                var firebaseRemoteConfigService = RemoteConfigManager.gameObject.AddComponent<FirebaseRemoteConfigService>();
                RemoteConfigManager.Initialize(firebaseRemoteConfigService, OnRemoteConfigFetchCompleted);
            }
        }

        private void InitializeServices()
        {
            if (PlayerPrefs.HasKey(PLAYER_PREFS_KEY_FIRST_OPEN))
            {
                IsFirstOpen = false;
            }
            else
            {
                PlayerPrefs.SetInt(PLAYER_PREFS_KEY_FIRST_OPEN, 1);
                PlayerPrefs.Save();
                IsFirstOpen = true;
            }

#if MOCALIB_USE_SERVER_TIME
            ServerTimeManager = gameObject.AddComponent<ServerTimeManager>();
            ServerTimeManager.Initialize();
#endif

            MMPManager = gameObject.AddComponent<MMPManager>();
            MMPManager.Initialize(_mmpConfig);

            FirebaseService = gameObject.AddComponent<FirebaseService>();
            FirebaseService.Initialize(() =>
            {
                var fiam = new GameObject(nameof(FIAMManager));
                FIAMManager = fiam.AddComponent<FIAMManager>();
                FIAMManager.Instance.Initialize();

#if MOCALIB_USE_FIREBASE_LEADERBOARD
                PlayerProfileManager = gameObject.AddComponent<PlayerProfileManager>();
                PlayerProfileManager.Initialize(_onPlayerProfileLoaded);

                LeaderboardManager = gameObject.AddComponent<LeaderboardManager>();
#endif

                if (_useFirebaseRemoteConfig && RemoteConfigManager == null)
                {
                    RemoteConfigManager = gameObject.AddComponent<RemoteConfigManager>();
                    var firebaseRemoteConfigService = RemoteConfigManager.gameObject.AddComponent<FirebaseRemoteConfigService>();
                    RemoteConfigManager.Initialize(firebaseRemoteConfigService, OnRemoteConfigFetchCompleted);
                }

                PushNotificationManager = gameObject.AddComponent<PushNotificationManager>();
                var firebasePushNotificationService = PushNotificationManager.gameObject.AddComponent<FirebasePushNotificationService>();
                PushNotificationManager.Initialize(firebasePushNotificationService);

                if (_useFirebaseAppInstanceIdAsAdUserId)
                    _ = InitAdManagerWithFirebaseAppInstanceId();

                _onFirebaseInitialized?.Invoke();
            });

            AnalyticsManager = gameObject.AddComponent<AnalyticsManager>();
            AnalyticsManager.AddService(new FirebaseAnalyticsService());

#if MOCALIB_USE_BYTEBREW
            AnalyticsManager.AddService(new ByteBrewAnalyticsService());
#endif
#if MOCALIB_USE_GAMEANALYTICS
            AnalyticsManager.AddService(new GameAnalyticsService());
#endif

            AnalyticsManager.Initialize();

            if (IsFirstOpen) AnalyticsManager.LogFirstOpen();
            AnalyticsManager.LogAppOpen();

            AdManager = gameObject.AddComponent<AdManager>();
            if (!_useFirebaseAppInstanceIdAsAdUserId)
            {
                InitAdManager();
            }

            IAPManager = gameObject.AddComponent<IAPManager>();
            IAPManager.SetConfig(_iapConfig);

            FacebookManager = gameObject.AddComponent<FacebookManager>();
            FacebookManager.Initialize();

            RatingManager = gameObject.AddComponent<RatingManager>();
            RatingManager.Initialize(_iOSAppId, _useInAppRating);
        }

        private void InitAdManager()
        {
            if (AdManager.IsInitialized)
            {
                Utils.MocaLibLogWarning("MocaLib", "AdManager init duplicated");
                return;
            }

#if UNITY_ANDROID
            AdManager.Initialize(_adConfigAndroid, _adEventsHandler);
#elif UNITY_IOS
            AdManager.Initialize(_adConfigIOS, _adEventsHandler);
#endif
        }

        // NOTICE: Firebase needs to be initialized before call this function
        private async Task InitAdManagerWithFirebaseAppInstanceId()
        {
            try
            {
                var task = FirebaseAnalytics.GetAnalyticsInstanceIdAsync();
                var timeout = 5f;
                while (!task.IsCompleted && timeout > 0f)
                {
                    await Task.Yield();
                    timeout -= Time.deltaTime;
                }

                if (task.IsCompleted)
                {
                    if (_adConfigAndroid != null)
                        _adConfigAndroid.UserId = task.Result;

                    if (_adConfigIOS != null)
                        _adConfigIOS.UserId = task.Result;
                }
                else
                {
                    Utils.MocaLibLogWarning("MocaLib", "AdManager is initialized without firebase app instance id (timeout)");
                }

                if (AdManager.IsInitialized)
                {
                    Utils.MocaLibLogWarning("MocaLib", "AdManager is initialized without firebase app instance id");
                    return;
                }

                InitAdManager();
            }
            catch (Exception e)
            {
                Utils.MocaLibLogError("MocaLib", $"Init AdManager with FAInstanceId get error: {e.Message}");
            }
            finally
            {
                if (!AdManager.IsInitialized)
                    InitAdManager();
            }
        }

#if UNITY_IOS && !UNITY_EDITOR
        private void RequestATTracking()
        {
            const string PLAYERPREFS_ATT_KEY = "Moca_ATT";

            if (!PlayerPrefs.HasKey(PLAYERPREFS_ATT_KEY) && Utils.Is_iOS_14_5_Or_Higher())
            {
                ATTrackingStatusBinding.RequestAuthorizationTracking();
                PlayerPrefs.SetInt(PLAYERPREFS_ATT_KEY, 1);
            }
        }
#endif
    }
}
