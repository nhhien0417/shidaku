using System;
using UnityEngine;

#if !UNITY_WEBGL
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
#if MOCALIB_USE_FIREBASE_APP_CHECK
using Firebase.AppCheck;
#endif
#endif

using Titipi.MocaLib.Runtime.Common;

namespace Titipi.MocaLib.Runtime.Services
{
    public class FirebaseService : MonoBehaviour
    {
        private const string TAG = "FirebaseService";

        public bool IsInitialized { get; private set; }

#if !UNITY_WEBGL
        // Keep a strong, app-lifetime reference to the default FirebaseApp so the
        // managed wrapper (and the native object Firestore/Auth depend on) is never
        // garbage collected.
        public FirebaseApp App { get; private set; }
#endif

        public void Initialize(Action callback)
        {
#if !UNITY_WEBGL
            if (IsInitialized) return;

            Utils.MocaLibLog(TAG, "Initialize");

#if MOCALIB_USE_FIREBASE_APP_CHECK
#if UNITY_ANDROID && !UNITY_EDITOR
            FirebaseAppCheck.SetAppCheckProviderFactory(PlayIntegrityProviderFactory.Instance);
#elif UNITY_IOS && !UNITY_EDITOR
            FirebaseAppCheck.SetAppCheckProviderFactory(DeviceCheckProviderFactory.Instance);
#endif
#endif

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    App = FirebaseApp.DefaultInstance;
                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                    IsInitialized = true;

                    callback?.Invoke();
                }
                else
                {
                    Utils.MocaLibLogError(TAG, $"Could not resolve all Firebase dependencies: {dependencyStatus}");
                    // Firebase Unity SDK is not safe to use here.
                }
            });
#endif
        }
    }
}
