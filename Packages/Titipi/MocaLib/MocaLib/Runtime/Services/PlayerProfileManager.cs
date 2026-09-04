#if MOCALIB_USE_FIREBASE_LEADERBOARD

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

using Titipi.MocaLib.Runtime.Common;

namespace Titipi.MocaLib.Runtime.Services
{
    [FirestoreData]
    [Serializable]
    public class PlayerProfile
    {
        [FirestoreProperty] public string UserId { get; set; }
        [FirestoreProperty] public string DisplayName { get; set; }
        [FirestoreProperty] public string Avatar { get; set; }
        [FirestoreProperty] public string AppVersion { get; set; }
        [FirestoreProperty] public string AppBuildNumber { get; set; }
        [FirestoreProperty] public string DevicePlatform { get; set; }
        [FirestoreProperty] public string DeviceLocale { get; set; }
        [FirestoreProperty] public string DeviceTimeZone { get; set; }
        [FirestoreProperty] public Timestamp CreatedAt { get; set; }
        [FirestoreProperty] public Timestamp UpdatedAt { get; set; }
        [FirestoreProperty] public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class PlayerProfileManager : MonoBehaviour
    {
        private const string TAG = "PlayerProfileManager";

        public PlayerProfile CurrentProfile { get; private set; } = new();
        public bool IsInitialized { get; private set; }

        private FirebaseFirestore _firestore;
        private string _userId;

        public void Initialize(Action<bool, string> onProfileLoaded)
        {
            if (IsInitialized) return;

            Utils.MocaLibLog(TAG, "Initialize");

            FirebaseAuthService.SignInAnonymously().ContinueWithOnMainThread(signInTask =>
            {
                if (signInTask.IsFaulted || signInTask.IsCanceled)
                {
                    Utils.MocaLibLogError(TAG, $"Anonymous sign-in failed: {signInTask.Exception}");
                    onProfileLoaded?.Invoke(false, signInTask.Exception?.Message);
                    return;
                }

                _firestore = FirebaseFirestore.DefaultInstance;
                _userId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;

                if (string.IsNullOrEmpty(_userId))
                {
                    const string err = "Firebase user not signed in.";
                    Utils.MocaLibLogError(TAG, err);
                    onProfileLoaded?.Invoke(false, err);
                    return;
                }

                FirebaseAuth.DefaultInstance.CurrentUser?.TokenAsync(true).ContinueWithOnMainThread(tokenTask =>
                {
                    if (tokenTask.IsFaulted || tokenTask.IsCanceled)
                    {
                        var err = $"Token refresh failed: {tokenTask.Exception}";
                        Utils.MocaLibLogError(TAG, err);
                        onProfileLoaded?.Invoke(false, err);
                        return;
                    }

                    LoadProfile(
                        onSuccess: () => onProfileLoaded?.Invoke(true, null),
                        onError: ex => onProfileLoaded?.Invoke(false, ex.Message)
                    );
                });
            });
        }

        public void LoadProfile(Action onSuccess, Action<Exception> onError)
        {
            StartCoroutine(LoadProfileWithRetryCoroutine(onSuccess, onError, 2));
        }

        private IEnumerator LoadProfileWithRetryCoroutine(Action onSuccess, Action<Exception> onError, int attemptsLeft)
        {
            yield return new WaitForSecondsRealtime(1.0f);

            var docRef = _firestore.Collection("users").Document(_userId);
            var task = docRef.GetSnapshotAsync();

            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted || task.IsCanceled)
            {
                var exception = task.Exception?.Flatten().InnerException;
                var isPermissionDenied = exception?.Message?.Contains("PERMISSION_DENIED") ?? false;

                if (isPermissionDenied && attemptsLeft > 0)
                {
                    Utils.MocaLibLogWarning(TAG, $"PERMISSION_DENIED — retrying LoadProfile... ({attemptsLeft} attempts left)");
                    yield return LoadProfileWithRetryCoroutine(onSuccess, onError, attemptsLeft - 1);
                    yield break;
                }

                onError?.Invoke(exception ?? new Exception("Unknown Firestore error"));
                yield break;
            }

            var snapshot = task.Result;
            if (snapshot.Exists)
            {
                try
                {
                    CurrentProfile = snapshot.ConvertTo<PlayerProfile>();
                }
                catch (Exception e)
                {
                    Utils.MocaLibLogError(TAG, $"Failed to convert profile data: {e.Message}");
                    onError?.Invoke(e);
                }

                IsInitialized = true;

                try
                {
                    onSuccess?.Invoke();
                }
                catch (Exception ex)
                {
                    Utils.MocaLibLogError(TAG, $"onSuccess callback threw: {ex}");
                }
            }
            else
            {
                Utils.MocaLibLog(TAG, "No profile found. Creating default profile.");

                var name = "Anonymous";
                var avatar = "local:0";
                var appVersion = Utils.GetAppVersion();
                var appBuildNumber = Utils.GetAppBuildNumber();
                var devicePlatform = Utils.GetPlatformFriendlyName();
                var deviceLocale = Utils.GetDeviceLocale();
                var deviceTimeZone = Utils.GetUTCString();
                var t = Timestamp.GetCurrentTimestamp();


                var docData = new Dictionary<string, object>
                {
                    { "UserId", _userId },
                    { "DisplayName", name },
                    { "Avatar", avatar },
                    { "AppVersion", appVersion },
                    { "AppBuildNumber", appBuildNumber },
                    { "DevicePlatform", devicePlatform },
                    { "DeviceLocale",  deviceLocale },
                    { "DeviceTimeZone", deviceTimeZone},
                    { "CreatedAt", t },
                    { "UpdatedAt", t },
                    { "Metadata", new Dictionary<string, object>() }
                };

                docRef.SetAsync(docData, SetOptions.Overwrite).ContinueWithOnMainThread(saveTask =>
                {
                    if (saveTask.IsFaulted || saveTask.IsCanceled)
                    {
                        onError?.Invoke(saveTask.Exception);
                        return;
                    }

                    CurrentProfile = new PlayerProfile
                    {
                        UserId = _userId,
                        DisplayName = name,
                        Avatar = avatar,
                        AppVersion = appVersion,
                        AppBuildNumber = appBuildNumber,
                        DevicePlatform =  devicePlatform,
                        DeviceLocale =  deviceLocale,
                        DeviceTimeZone =  deviceTimeZone,
                        CreatedAt = t,
                        UpdatedAt = t,
                        Metadata = new Dictionary<string, object>()
                    };

                    Utils.MocaLibLog(TAG, "Default profile created.");

                    IsInitialized = true;
                    onSuccess?.Invoke();
                });
            }
        }

        public void SaveProfile(Action onSuccess, Action<Exception> onError)
        {
            if (string.IsNullOrEmpty(_userId))
            {
                onError?.Invoke(new Exception("UserId is empty."));
                return;
            }

            CurrentProfile.UpdatedAt = Timestamp.GetCurrentTimestamp();
            CurrentProfile.AppVersion = Utils.GetAppVersion();
            CurrentProfile.AppBuildNumber = Utils.GetAppBuildNumber();
            CurrentProfile.DeviceLocale = Utils.GetDeviceLocale();
            CurrentProfile.DeviceTimeZone = Utils.GetUTCString();

            var docRef = _firestore.Collection("users").Document(_userId);
            docRef.SetAsync(CurrentProfile, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    onError?.Invoke(task.Exception);
                    return;
                }

                Utils.MocaLibLog(TAG, "Profile saved.");
                onSuccess?.Invoke();
            });
        }

        public void SetDisplayName(string newName) =>
            CurrentProfile.DisplayName = string.IsNullOrWhiteSpace(newName) ? "Anonymous" : newName;

        public void SetLocalAvatar(int avatarId) => CurrentProfile.Avatar = $"local:{avatarId}";

        public void SetAvatarUrl(string url) => CurrentProfile.Avatar = url;

        public string GetAvatar() => CurrentProfile.Avatar;

        public bool IsLocalAvatar() => CurrentProfile.Avatar?.StartsWith("local:") ?? false;

        public int GetLocalAvatarId()
        {
            if (IsLocalAvatar() && int.TryParse(CurrentProfile.Avatar.Substring(6), out var id))
                return id;
            return -1;
        }

        public void SetMetadataValue(string key, object value)
        {
            CurrentProfile.Metadata ??= new Dictionary<string, object>();
            CurrentProfile.Metadata[key] = value;
        }

        public T GetMetadataValue<T>(string key, T defaultValue = default)
        {
            if (CurrentProfile.Metadata != null && CurrentProfile.Metadata.TryGetValue(key, out var val))
            {
                try
                {
                    if (val is T tVal)
                        return tVal;

                    return (T)Convert.ChangeType(val, typeof(T));
                }
                catch (Exception e)
                {
                    Utils.MocaLibLogError(TAG, $"Failed to convert metadata key '{key}' value '{val}': {e.Message}");
                }
            }

            return defaultValue;
        }
    }
}

#endif
