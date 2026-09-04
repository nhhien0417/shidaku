using System;
using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

using Titipi.MocaLib.Runtime.Common;

namespace Titipi.MocaLib.Runtime.Services
{
    public class PushNotificationPermissionHandler : MonoBehaviour
    {
        private const string TAG = "PushNotificationPermissionHandler";

        public Action OnPermissionGrantedAction;
        public Action OnPermissionDeniedAction;

        public void RequestNotificationPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
            {
                Debug.Log("Android notification permission already granted.");
                OnPermissionGrantedAction?.Invoke();
                return;
            }

            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += OnPermissionGranted;
            callbacks.PermissionDenied += OnPermissionDenied;
            Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS", callbacks);
#elif UNITY_IOS && !UNITY_EDITOR
            OnPermissionGrantedAction?.Invoke();
#else
            // Editor or other platforms: Assume granted for testing
            Utils.MocaLibLog(TAG, "Non-mobile platform: Assuming permission granted.");
            OnPermissionGrantedAction?.Invoke();
#endif
        }

        private void OnPermissionGranted(string permission)
        {
            Utils.MocaLibLog(TAG, $"Android permission `{permission}` granted!");
            OnPermissionGrantedAction?.Invoke();
        }

        private void OnPermissionDenied(string permission)
        {
            Utils.MocaLibLog(TAG, $"Android permission `{permission}` denied!");
            OnPermissionDeniedAction?.Invoke();
        }
    }
}
