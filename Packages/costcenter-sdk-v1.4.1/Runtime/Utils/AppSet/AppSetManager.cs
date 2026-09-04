using UnityEngine;
using System;

namespace CostCenter
{
    public class AppSetIdManager
    {
        private const string JavaClassName = "com.ExtraLabs.unitysdk.AppSetIdPlugin";

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaClass pluginClass;
    private static AndroidJavaObject unityActivity;
#endif

        private static AppSetIdManager _instance;
        public static AppSetIdManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AppSetIdManager();
                return _instance;
            }
        }

        private AppSetIdManager()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (pluginClass == null)
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }
            pluginClass = new AndroidJavaClass(JavaClassName);
            pluginClass.CallStatic("setActivity", unityActivity);
        }
#endif
        }

        /// <summary>
        /// Lấy App Set ID, callback trả về nullable string
        /// </summary>
        public void GetAppSetId(Action<string> callback)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        var receiver = AppSetIdCallbackReceiver.Create(callback);
        pluginClass.CallStatic("getAppSetId", receiver.gameObject.name, "OnAppSetIdReceived");
#else
            callback?.Invoke(null);
#endif
        }

        /// <summary>
        /// Class này tạo GameObject tạm để nhận callback từ Java
        /// </summary>
        private class AppSetIdCallbackReceiver : MonoBehaviour
        {
            private static AppSetIdCallbackReceiver I;

            private Action<string> _callback;

            public static AppSetIdCallbackReceiver Create(Action<string> callback)
            {
                if (I == null)
                {
                    var go = new GameObject("AppSetIdCallbackReceiver");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    I = go.AddComponent<AppSetIdCallbackReceiver>();
                    I._callback = callback;
                }

                return I;
            }

            public void OnAppSetIdReceived(string appSetId)
            {
                _callback?.Invoke(appSetId);
            }
        }
    }
}
