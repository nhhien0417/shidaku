using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;
using UnityEngine.Networking;
using Ugi.PlayInstallReferrerPlugin;

namespace CostCenter.Attribution {
    public class CCTracking
    {
        // private const int MAXIMUM_RETRY = 5;
        // internal static bool IsFirstOpen {
        //     get {
        //         if (PlayerPrefs.HasKey(CCConstant.FIRST_OPEN_KEY)) {
        //             int isFirst = PlayerPrefs.GetInt(CCConstant.FIRST_OPEN_KEY);
        //             return isFirst <= 0 && isFirst > -MAXIMUM_RETRY;
        //         }
        //         return true;
        //     }
        //     set {
        //         if (!value) {
        //             PlayerPrefs.SetInt(CCConstant.FIRST_OPEN_KEY, 1);
        //             return;
        //         }
        //         if (!PlayerPrefs.HasKey(CCConstant.FIRST_OPEN_KEY)) {
        //             PlayerPrefs.SetInt(CCConstant.FIRST_OPEN_KEY, 0);
        //             return;
        //         }
        //         int lastFirst = PlayerPrefs.GetInt(CCConstant.FIRST_OPEN_KEY);
        //         PlayerPrefs.SetInt(CCConstant.FIRST_OPEN_KEY, lastFirst - 1);
        //     }
        // }
        internal static bool IsTrackedATT {
            get {
                return PlayerPrefs.GetInt(CCConstant.TRACKED_ATT_KEY, 0) == 1;
            }
            set {
                PlayerPrefs.SetInt(CCConstant.TRACKED_ATT_KEY, value ? 1 : 0);
            }
        }
        internal static bool IsTrackedMMP {
            get {
                return PlayerPrefs.GetInt(CCConstant.TRACKED_MMP_KEY, 0) == 1;
            }
            set {
                PlayerPrefs.SetInt(CCConstant.TRACKED_MMP_KEY, value ? 1 : 0);
            }
        }

        private static Dictionary<string, object> _installReferrerInfo = null;
        private static string _idfv = null;
        private static string _firebaseAppInstanceId = string.Empty;

        #if UNITY_IOS && !UNITY_EDITOR
        [DllImport ("__Internal")]
	    private static extern string _CCGetAttributionToken();
        #endif

        internal static IEnumerator AppOpen(string firebaseAppInstanceId = null, float delayTime = 1.0f)
        {
            yield return new WaitUntil(() => CCFirebase.IsInitialized);

            yield return new WaitForSeconds(delayTime);
            // _firebaseAppInstanceId = string.IsNullOrEmpty(firebaseAppInstanceId) ? _firebaseAppInstanceId : firebaseAppInstanceId;
            // if (string.IsNullOrEmpty(_firebaseAppInstanceId)) {
            //     System.Threading.Tasks.Task<string> task = Firebase.Analytics.FirebaseAnalytics.GetAnalyticsInstanceIdAsync();
            //     yield return new WaitUntil(() => task.IsCompleted);
            //     _firebaseAppInstanceId = task.Result;
            // }
            System.Threading.Tasks.Task<string> task = null;
            _firebaseAppInstanceId = string.IsNullOrEmpty(firebaseAppInstanceId) ? _firebaseAppInstanceId : firebaseAppInstanceId;
            try
            {
                if (string.IsNullOrEmpty(_firebaseAppInstanceId))
                {
                    task = Firebase.Analytics.FirebaseAnalytics.GetAnalyticsInstanceIdAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"CC Tracking AppOpen: Failed to set user property 'attribution_id'. {ex.Message}");
                yield break;
            }

            if (task != null)
            {
                yield return new WaitUntil(() => task.IsCompleted);
                _firebaseAppInstanceId = task.Result;
            }

            string bundleId = Application.identifier;
            string platform = Application.platform == RuntimePlatform.Android ? "android" : "ios";

            string url = "https://attribution.costcenter.net/appopen?";
            url += $"bundle_id={bundleId}";
            url += $"&platform={platform}";
            if (!string.IsNullOrEmpty(_firebaseAppInstanceId)) {
                url += $"&firebase_app_instance_id={_firebaseAppInstanceId}";
            }
            GetIDFV();
            yield return new WaitUntil(() => !string.IsNullOrEmpty(_idfv));
            url += $"&vendor_id={UnityWebRequest.EscapeURL(_idfv)}";
            #if UNITY_ANDROID && !UNITY_EDITOR
                url += $"&advertising_id={UnityWebRequest.EscapeURL(GetIDFA())}";
            #endif
            Firebase.Analytics.FirebaseAnalytics.SetUserProperty("vendor_id", _idfv);
            
            // ANDROID INSTALL REFERRER
            _installReferrerInfo = null;
            #if UNITY_ANDROID && !UNITY_EDITOR
                PlayInstallReferrerAndroid.GetInstallReferrerInfo(InstallReferrerCallback);
                yield return new WaitUntil(() => _installReferrerInfo != null);
            #elif UNITY_EDITOR
                PlayInstallReferrerEditor.GetInstallReferrerInfo(InstallReferrerCallback);
                yield return new WaitUntil(() => _installReferrerInfo != null);
            #endif
            if (_installReferrerInfo != null) {
                foreach (KeyValuePair<string, object> info in _installReferrerInfo) {
                    string value = $"{info.Key}" == "install_referrer"
                        ? UnityWebRequest.EscapeURL(info.Value.ToString())
                        : info.Value.ToString();
                    url += $"&{info.Key}={value}";
                }
            }

            // IOS ATTRIBUTION TOKEN
            #if UNITY_IOS && !UNITY_EDITOR
                string attributionToken = _CCGetAttributionToken();
                if (!string.IsNullOrEmpty(attributionToken)) {
                    url += $"&attribution_token={UnityWebRequest.EscapeURL(attributionToken)}";
                }
            #endif

            Debug.Log($"CC Tracking AppOpen: {url}");

            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError) {
                Debug.Log(www.error);
            } else {
                Debug.Log("CCAttribution CallAppOpen: success");
            }
        }

        internal static void InstallReferrerCallback(PlayInstallReferrerDetails installReferrerDetails) {
            Dictionary<string, object> result = new Dictionary<string, object>();
            Debug.Log("Install referrer details received!");

            // check for error
            if (installReferrerDetails.Error != null)
            {
                Debug.LogError("Error occurred!");
                if (installReferrerDetails.Error.Exception != null)
                {
                    Debug.LogError("Exception message: " + installReferrerDetails.Error.Exception.Message);
                }
                Debug.LogError("Response code: " + installReferrerDetails.Error.ResponseCode.ToString());
                _installReferrerInfo = result;
                return;
            }

            // print install referrer details
            if (installReferrerDetails.InstallReferrer != null)
            {
                result["install_referrer"] = installReferrerDetails.InstallReferrer;
                Debug.Log("Install referrer: " + installReferrerDetails.InstallReferrer);
            }
            // if (installReferrerDetails.ReferrerClickTimestampSeconds != null)
            // {
            //     result["click_ts"] = installReferrerDetails.ReferrerClickTimestampSeconds.ToString();
            //     Debug.Log("Referrer click timestamp: " + installReferrerDetails.ReferrerClickTimestampSeconds);
            // }
            // if (installReferrerDetails.InstallBeginTimestampSeconds != null)
            // {
            //     result["install_ts"] = installReferrerDetails.InstallBeginTimestampSeconds.ToString();
            //     Debug.Log("Install begin timestamp: " + installReferrerDetails.InstallBeginTimestampSeconds);
            // }
            if (installReferrerDetails.ReferrerClickTimestampServerSeconds != null)
            {
                result["click_ts"] = installReferrerDetails.ReferrerClickTimestampServerSeconds.ToString();
                Debug.Log("Referrer click server timestamp: " + installReferrerDetails.ReferrerClickTimestampServerSeconds);
            }
            if (installReferrerDetails.InstallBeginTimestampServerSeconds != null)
            {
                result["install_ts"]  = installReferrerDetails.InstallBeginTimestampServerSeconds.ToString();
                Debug.Log("Install begin server timestamp: " + installReferrerDetails.InstallBeginTimestampServerSeconds);
            }
            // if (installReferrerDetails.InstallVersion != null)
            // {
            //     result["install_version"] = installReferrerDetails.InstallVersion;
            //     Debug.Log("Install version: " + installReferrerDetails.InstallVersion);
            // }
            // if (installReferrerDetails.GooglePlayInstant != null)
            // {
            //     txtGooglePlayInstantFromCallback = installReferrerDetails.GooglePlayInstant.ToString();
            //     Debug.Log("Google Play instant: " + installReferrerDetails.GooglePlayInstant);
            // }
            _installReferrerInfo = result;
        }

        internal static IEnumerator TrackATT(string firebaseAppInstanceId = null, float delayTime = 5.0f)
        {
            yield return new WaitUntil(() => CCFirebase.IsInitialized);
            
            yield return new WaitForSeconds(delayTime);

            string idfa = GetIDFA();
            Debug.Log($"CC Tracking IDFA: {idfa}");
            if (string.IsNullOrEmpty(idfa) || idfa == "00000000-0000-0000-0000-000000000000") {
                yield break;
            }

            // string fbAppInstanceId = firebaseAppInstanceId;
            // if (string.IsNullOrEmpty(fbAppInstanceId)) {
            //     System.Threading.Tasks.Task<string> task = Firebase.Analytics.FirebaseAnalytics.GetAnalyticsInstanceIdAsync();
            //     yield return new WaitUntil(() => task.IsCompleted);
            //     fbAppInstanceId = task.Result;
            // }
            System.Threading.Tasks.Task<string> task = null;
            _firebaseAppInstanceId = string.IsNullOrEmpty(firebaseAppInstanceId) ? _firebaseAppInstanceId : firebaseAppInstanceId;
            try
            {
                if (string.IsNullOrEmpty(_firebaseAppInstanceId))
                {
                    task = Firebase.Analytics.FirebaseAnalytics.GetAnalyticsInstanceIdAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"CC Tracking ATT: Failed to set user property 'attribution_id'. {ex.Message}");
                yield break;
            }

            if (task != null)
            {
                yield return new WaitUntil(() => task.IsCompleted);
                _firebaseAppInstanceId = task.Result;
            }

            string bundleId = Application.identifier;
            string platform = Application.platform == RuntimePlatform.Android ? "android" : "ios";

            string url = "https://attribution.costcenter.net/appopen?";
            url += $"bundle_id={bundleId}";
            url += $"&platform={platform}";
            if (!string.IsNullOrEmpty(_firebaseAppInstanceId)) {
                url += $"&firebase_app_instance_id={_firebaseAppInstanceId}";
            }
            // url += $"&vendor_id={UnityWebRequest.EscapeURL(GetIDFV())}";
            url += $"&advertising_id={UnityWebRequest.EscapeURL(idfa)}";

            Debug.Log($"CC Tracking ATT url: {url}");

            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError) {
                Debug.Log(www.error);
            } else {
                Debug.Log("CC Tracking ATT: success");
                IsTrackedATT = true;
            }

        }

        #if UNITY_IOS && !UNITY_EDITOR
        [DllImport ("__Internal")]
	    private static extern string _CCGetIDFA();
        #endif
        public static string GetIDFA()
        {
            string advertisingID = "";
            #if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    AndroidJavaClass up = new AndroidJavaClass ("com.unity3d.player.UnityPlayer");
                    AndroidJavaObject currentActivity = up.GetStatic<AndroidJavaObject> ("currentActivity");
                    AndroidJavaClass client = new AndroidJavaClass ("com.google.android.gms.ads.identifier.AdvertisingIdClient");
                    AndroidJavaObject adInfo = client.CallStatic<AndroidJavaObject> ("getAdvertisingIdInfo", currentActivity);
            
                    advertisingID = adInfo.Call<string> ("getId").ToString();
                }
                catch (Exception e)
                {
                    Debug.Log($"CC Tracking GetIDFA Android: {e.ToString()}");
                }
                
            #elif UNITY_IOS && !UNITY_EDITOR
                string idfa = _CCGetIDFA();
                if (!string.IsNullOrEmpty(idfa)) {
                    advertisingID = idfa;
                }
            #endif
            return advertisingID;
        }

        public static void GetIDFV() {
            #if UNITY_ANDROID && !UNITY_EDITOR
                AppSetIdManager.Instance.GetAppSetId((appSetId) =>
                {
                    _idfv = appSetId;
                });
            #else
                _idfv = SystemInfo.deviceUniqueIdentifier;
            #endif
            
        }

        internal static IEnumerator TrackMMP(string attributionId, string firebaseAppInstanceId = null, float delayTime = 15.0f)
        {
            yield return new WaitUntil(() => CCFirebase.IsInitialized);

            yield return new WaitForSeconds(delayTime);

            System.Threading.Tasks.Task<string> task = null;
            try
            {
                Firebase.Analytics.FirebaseAnalytics.SetUserProperty("attribution_id", attributionId);
                _firebaseAppInstanceId = string.IsNullOrEmpty(firebaseAppInstanceId) ? _firebaseAppInstanceId : firebaseAppInstanceId;
                if (string.IsNullOrEmpty(_firebaseAppInstanceId))
                {
                    task = Firebase.Analytics.FirebaseAnalytics.GetAnalyticsInstanceIdAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"CC Tracking MMP: Failed to set user property 'attribution_id'. {ex.Message}");
                yield break;
            }

            if (task != null)
            {
                yield return new WaitUntil(() => task.IsCompleted);
                _firebaseAppInstanceId = task.Result;
            }

            string bundleId = Application.identifier;
            string platform = Application.platform == RuntimePlatform.Android ? "android" : "ios";

            string url = "https://attribution.costcenter.net/appopen?";
            url += $"bundle_id={bundleId}";
            url += $"&platform={platform}";
            if (!string.IsNullOrEmpty(_firebaseAppInstanceId))
            {
                url += $"&firebase_app_instance_id={_firebaseAppInstanceId}";
            }
            // url += $"&vendor_id={UnityWebRequest.EscapeURL(GetIDFV())}";
#if UNITY_ANDROID && !UNITY_EDITOR
                url += $"&advertising_id={UnityWebRequest.EscapeURL(GetIDFA())}";
#endif
            url += $"&attribution_id={attributionId}";

            Debug.Log($"CC Tracking MMP: {url}");

            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("CCAttribution CallTrackMMP: success");
                IsTrackedMMP = true;
            }
        }
    }
}
