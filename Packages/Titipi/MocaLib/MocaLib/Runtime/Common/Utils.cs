using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Titipi.MocaLib.Runtime.Common
{
    public static class Utils
    {
        private const string MOCALIB_TAG = "MocaLib";

        public static void MocaLibLog(string tag, string s)
        {
#if UNITY_EDITOR
            Debug.Log($"<color=cyan>{MOCALIB_TAG}::{tag} -> {s}</color>");
#elif MOCALIB_ENABLE_INFO_LOG
            Debug.Log($"{MOCALIB_TAG}::{tag} -> {s}");
#endif
        }

        public static void MocaLibLogWarning(string tag, string s)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"<color=cyan>{MOCALIB_TAG}::{tag} -> {s}</color>");
#else
            Debug.LogWarning($"{MOCALIB_TAG}::{tag} -> {s}");
#endif
        }

        public static void MocaLibLogError(string tag, string s)
        {
#if UNITY_EDITOR
            Debug.LogError($"<color=cyan>{MOCALIB_TAG}::{tag} -> {s}</color>");
#else
            Debug.LogError($"{MOCALIB_TAG}::{tag} -> {s}");
#endif
        }

        public static string NormalizeTrackingEventName(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                return string.Empty;

            // Replace underscores with space (unify separators)
            var normalized = eventName.Replace("_", " ");

            // Split camelCase and PascalCase including acronym boundaries
            normalized = Regex.Replace(normalized, @"(?<=[a-z0-9])([A-Z])", " $1");
            normalized = Regex.Replace(normalized, @"(?<=[A-Z])([A-Z][a-z])", " $1");

            // Remove non-alphanumeric characters (except spaces)
            normalized = Regex.Replace(normalized, @"[^a-zA-Z0-9 ]+", "");

            // Trim, lowercase, and replace space with underscore
            return normalized.Trim().ToLower().Replace(" ", "_");
        }

        public static string GetPlatformFriendlyName()
        {
            var p = Application.platform;

            return p switch
            {
                RuntimePlatform.Android => "Android",
                RuntimePlatform.IPhonePlayer => "iOS",
                RuntimePlatform.WindowsPlayer => "Windows",
                RuntimePlatform.OSXPlayer => "MacOS",
                RuntimePlatform.LinuxPlayer => "Linux",
                RuntimePlatform.OSXEditor => "Unity Editor (macOS)",
                RuntimePlatform.WindowsEditor => "Unity Editor (Windows)",
                RuntimePlatform.LinuxEditor => "Unity Editor (Linux)",
                _ => p.ToString()
            };
        }

        public static string GetAppVersion()
        {
            return $"{GameVersionInfo.BUILD_VERSION}";
        }

        public static string GetAppBuildNumber()
        {
            return $"{GameVersionInfo.BUILD_NUMBER}";
        }

        public static string GetUTCString()
        {
            var offset = TimeZoneInfo.Local.BaseUtcOffset;
            var hours = offset.Hours;
            var minutes = offset.Minutes;

            var sign = offset >= TimeSpan.Zero ? "+" : "-";
            var formattedOffset = minutes == 0
                ? $"UTC{sign}{Math.Abs(hours)}"
                : $"UTC{sign}{Math.Abs(hours)}:{Math.Abs(minutes):D2}";

            return formattedOffset;
        }

        public static string GetDeviceLocale()
        {
            return RegionInfo.CurrentRegion.TwoLetterISORegionName;
        }

        public static string GetAndroidReceiptSignature(Product product)
        {
            var receiptAndroid = JsonUtility.FromJson<Receipt>(product.receipt);
            var receiptPayload = JsonUtility.FromJson<PayloadAndroid>(receiptAndroid.Payload);

            return receiptPayload.Signature;
        }

        public static string GetIOSReceiptPayload(Product product)
        {
            var receiptIOS = JsonUtility.FromJson<Receipt>(product.receipt);

            return receiptIOS.Payload;
        }

        public static bool Is_iOS_14_5_Or_Higher()
        {
#if UNITY_IOS && !UNITY_EDITOR
        var (verMajor, verMinor) = Get_iOS_Version();
        return verMajor > 14 || (verMajor == 14 && verMinor >= 5);
#else
            return false;
#endif
        }

        public static bool Is_iOS_17_Or_Higher()
        {
#if UNITY_IOS && !UNITY_EDITOR
        var (major, minor) = Get_iOS_Version();
        return major >= 17;
#else
            return false;
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
    private static (int verMajor, int verMinor) Get_iOS_Version()
    {
        int major = 0;
        int minor = 0;

        string[] v = UnityEngine.iOS.Device.systemVersion.Split('.');

        if (v.Length >= 2)
        {
            int.TryParse(v[0], out major);
            int.TryParse(v[1], out minor);
        }

        return (major, minor);
    }
#endif
    }

    public class Receipt
    {
        public string Store;
        public string TransactionID;
        public string Payload;

        public Receipt()
        {
            Store = TransactionID = Payload = "";
        }

        public Receipt(string store, string transactionID, string payload)
        {
            Store = store;
            TransactionID = transactionID;
            Payload = payload;
        }
    }

    public class PayloadAndroid
    {
        public string Json;
        public string Signature;

        public PayloadAndroid()
        {
            Json = Signature = "";
        }

        public PayloadAndroid(string json, string signature)
        {
            Json = json;
            Signature = signature;
        }
    }
}
