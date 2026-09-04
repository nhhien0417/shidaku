using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

using Titipi.MocaLib.Runtime.Common;
using Titipi.MocaLib.Runtime.Services.Internal;

namespace Titipi.MocaLib.Runtime.Services
{
    public enum PopupButtonActionType
    {
        None,
        Dismiss,
        OpenScreen,
        OpenURL,
        RateApp,
        Purchase,
        PurchaseWithCoins,
        PurchaseWithGems,
        ShowInterstitial,
        ShowRewardedVideo
    }

    public struct Metadata
    {
        public string CampaignName;
        public string ImageUrl;
        public string Rewards;
        public string IconUrl;
        public double? CampaignDurationInHours;
        public DateTime? ReceivedAtUtc;
    }

    public struct PopupButton
    {
        public string Text;
        public PopupButtonActionType ActionType;
        public string ActionParam;
        public Metadata? Metadata;
    }

    public struct ModalMessageData
    {
        public string CampaignName;
        public string Title;
        public string Message;
        public PopupButton? OkButton;
        public PopupButton? CloseButton;
    }

    public struct ImageMessageData
    {
        public PopupButton? OkButton;
        public PopupButton? CloseButton;
        public Metadata Metadata;
    }

    public struct BannerMessageData
    {
        public string CampaignName;
        public Dictionary<string, string> CustomData;
    }

    public class FIAMManager : MonoSingleton<FIAMManager>
    {
        private const string TAG = "FIAMManager";

        private readonly Queue<object> _messageQueue = new();
        private bool _isProcessing;

        private Action<ModalMessageData?> _onModalMessageReceived;
        private Action<ImageMessageData> _onImageMessageReceived;
        private Action<BannerMessageData> _onBannerMessageReceived;

        private const string STORAGE_PATH = "stored_fiam_campaigns.json";
        private const string CAMPAIGN_MEDIA_FOLDER = "campaign_media";

        private const string PLUGIN_CLASS = "com.titipigames.mocalib.plugin.fiam.FIAMPlugin";

        /// <summary>
        /// This is for handling the behavior of the Ok button and Close button of the Modal and Image In-App Messages.
        /// Developers will need to implement the corresponding action for each button.
        /// </summary>
        /// <param name="handler"></param>
        public void RegisterPopupHandler(IFIAMPopupHandler handler)
        {
            _onModalMessageReceived += modalData => FIAMPopup.ShowModalPopup(modalData, handler);
            _onImageMessageReceived += imageData => FIAMPopup.ShowImagePopup(imageData, handler);
        }

        /// <summary>
        /// This is for receiving the Banner In-App Messages, which doesn't use popup;
        /// Instead, developers will manually process the message for custom needs.
        /// </summary>
        public void RegisterBannerMessageReceivedEvent(Action<BannerMessageData> onBannerMessageReceived)
        {
            _onBannerMessageReceived += onBannerMessageReceived;
        }

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void FIAMPlugin_Initialize();
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void FIAMPlugin_OnOk();
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void FIAMPlugin_OnClose();
#endif

        public void Initialize()
        {
#if UNITY_WEBGL
            return;
#endif
            Utils.MocaLibLog(TAG, "Initialize");

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var fiamPlugin = new AndroidJavaClass(PLUGIN_CLASS))
            {
                fiamPlugin.CallStatic("initialize", activity);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            FIAMPlugin_Initialize();
#endif

            // Ensure campaign media folder exists
            Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, CAMPAIGN_MEDIA_FOLDER));
        }

        public void OnOkClick()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var fiamPlugin = new AndroidJavaClass(PLUGIN_CLASS))
            {
                fiamPlugin.CallStatic("onOk");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            FIAMPlugin_OnOk();
#endif
        }

        public void OnCloseClick()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var fiamPlugin = new AndroidJavaClass(PLUGIN_CLASS))
            {
                fiamPlugin.CallStatic("onClose");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            FIAMPlugin_OnClose();
#endif
        }

        public void OnMessageReceived(string json)
        {
            Utils.MocaLibLog(TAG, "OnMessageReceived");

            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            if (!dict.TryGetValue("Type", out var type))
            {
                Utils.MocaLibLogWarning(TAG, "Missing message type");
                return;
            }

            Utils.MocaLibLog(TAG, $"Message type: {type}");

            switch (type)
            {
                case "Modal":
                    var modal = ParseModalMessage(dict);
                    if (modal.HasValue)
                    {
                        PrintModalMessageInfo(modal.Value);
                        _messageQueue.Enqueue(modal.Value);
                    }
                    break;

                case "Image":
                    var image = ParseImageMessage(dict);
                    if (image.HasValue)
                    {
                        PrintImageMessageInfo(image.Value);
                        _messageQueue.Enqueue(image.Value);

                        var saved = LoadCampaigns();
                        saved.RemoveAll(x => x.Metadata.CampaignName == image.Value.Metadata.CampaignName);

                        // set ReceivedAtUtc now so we can compute expiry later
                        var newCampaign = image.Value;
                        newCampaign.Metadata.ReceivedAtUtc = DateTime.UtcNow;
                        saved.Add(newCampaign);
                        SaveCampaigns(saved);

                        StartCoroutine(DownloadAndCacheCampaignMediaCoroutine(newCampaign));
                    }
                    break;

                case "Banner":
                    var banner = ParseBannerMessage(dict);
                    if (banner.HasValue)
                    {
                        PrintBannerMessageInfo(banner.Value);
                        _messageQueue.Enqueue(banner.Value);
                    }
                    break;
            }

            TryProcessNext();
        }

        private void TryProcessNext()
        {
            if (_isProcessing || _messageQueue.Count == 0)
                return;

            _isProcessing = true;

            var msg = _messageQueue.Dequeue();
            switch (msg)
            {
                case ModalMessageData modal:
                    _onModalMessageReceived?.Invoke(modal);
                    break;
                case ImageMessageData image:
                    _onImageMessageReceived?.Invoke(image);
                    break;
                case BannerMessageData banner:
                    _onBannerMessageReceived?.Invoke(banner);
                    break;
            }

            _isProcessing = false;
        }

        private ModalMessageData? ParseModalMessage(Dictionary<string, string> dict)
        {
            dict.TryGetValue("CustomData__CampaignName", out var campaign);

            dict.TryGetValue("Title", out var title);
            dict.TryGetValue("Body", out var body);
            dict.TryGetValue("ButtonText", out var okLabel);

            var okRaw = Get(dict, "CustomData__OkAction");
            var closeRaw = Get(dict, "CustomData__CloseAction");

            var okButton = ParseButton(okLabel, okRaw);
            var closeButton = ParseButton("Close", closeRaw);

            return new ModalMessageData
            {
                CampaignName = campaign ?? "",
                Title = title ?? "",
                Message = body ?? "",
                OkButton = okButton,
                CloseButton = closeButton
            };
        }

        private ImageMessageData? ParseImageMessage(Dictionary<string, string> dict)
        {
            dict.TryGetValue("CustomData__CampaignName", out var campaign);
            dict.TryGetValue("CustomData__ImageUrl", out var imageUrl);

            var okRaw = Get(dict, "CustomData__OkAction");
            var closeRaw = Get(dict, "CustomData__CloseAction");
            var rewardsRaw = Get(dict, "CustomData__Rewards");
            var iconUrl = Get(dict, "CustomData__Icon");
            var durationStr = Get(dict, "CustomData__CampaignDurationInHours");

            double? campaignDuration = null;
            if (double.TryParse(durationStr, out var hours))
            {
                campaignDuration = hours;
            }

            var okButton = ParseButton("", okRaw) ?? new PopupButton { Text = "", ActionType = PopupButtonActionType.None, ActionParam = null };
            var closeButton = ParseButton("Close", closeRaw);

            var metadata = new Metadata
            {
                CampaignName = campaign ?? "",
                ImageUrl = imageUrl ?? "",
                Rewards = rewardsRaw,
                IconUrl = iconUrl,
                CampaignDurationInHours = campaignDuration,
                ReceivedAtUtc = null
            };

            return new ImageMessageData
            {
                OkButton = okButton,
                CloseButton = closeButton,
                Metadata = metadata
            };
        }

        private BannerMessageData? ParseBannerMessage(Dictionary<string, string> dict)
        {
            const string PREFIX = "CustomData__";

            dict.TryGetValue($"{PREFIX}CampaignName", out var campaign);

            var customData = dict
                .Where(kvp => kvp.Key.StartsWith(PREFIX) && kvp.Key != $"{PREFIX}CampaignName")
                .ToDictionary(
                    kvp => kvp.Key.Substring(PREFIX.Length),
                    kvp => kvp.Value
                );

            return new BannerMessageData
            {
                CampaignName = campaign ?? "",
                CustomData = customData
            };
        }

        private PopupButton? ParseButton(string label, string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;

            var parts = raw.Split(new[] { ':' }, 2);
            if (!Enum.TryParse(parts[0], out PopupButtonActionType type)) return null;

            return new PopupButton
            {
                Text = label,
                ActionType = type,
                ActionParam = parts.Length > 1 ? parts[1] : null
            };
        }

        private string Get(Dictionary<string, string> dict, string key)
        {
            dict.TryGetValue(key, out var val);
            return val;
        }

        private void PrintModalMessageInfo(ModalMessageData data)
        {
            Utils.MocaLibLog(TAG,
                $"Modal message info:\n" +
                $"    Title: {data.Title}\n" +
                $"    Message: {data.Message}\n" +
                $"    Ok button: {data.OkButton?.ActionType}\n" +
                $"    Ok button param: {data.OkButton?.ActionParam}\n" +
                $"    Close button: {data.CloseButton?.ActionType}\n" +
                $"    Close button param: {data.CloseButton?.ActionParam}"
            );
        }

        private void PrintImageMessageInfo(ImageMessageData data)
        {
            Utils.MocaLibLog(TAG,
                "Image message info:\n" +
                $"    Campaign: {data.Metadata.CampaignName}\n" +
                $"    Image URL: {data.Metadata.ImageUrl}\n" +
                $"    Icon URL: {data.Metadata.IconUrl}\n" +
                $"    Campaign Duration (hrs): {data.Metadata.CampaignDurationInHours}\n" +
                $"    ReceivedAtUtc: {data.Metadata.ReceivedAtUtc}\n" +
                $"    Ok button: {data.OkButton?.ActionType}\n" +
                $"    Ok button param: {data.OkButton?.ActionParam}\n" +
                $"    Rewards data: {data.Metadata.Rewards}\n" +
                $"    Close button: {data.CloseButton?.ActionType}\n" +
                $"    Close button param: {data.CloseButton?.ActionParam}"
            );
        }

        private void PrintBannerMessageInfo(BannerMessageData data)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("Banner message info:");
            sb.AppendLine($"    Campaign: {data.CampaignName}");

            foreach (var kv in data.CustomData)
            {
                sb.AppendLine($"    {kv.Key}: {kv.Value}");
            }

            Utils.MocaLibLog(TAG, sb.ToString());
        }

        [Serializable]
        private class StoredCampaigns
        {
            public List<ImageMessageData> Campaigns = new();
        }

        public void SaveCampaigns(List<ImageMessageData> campaigns)
        {
            var data = new StoredCampaigns { Campaigns = campaigns };
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(Path.Combine(Application.persistentDataPath, STORAGE_PATH), json);
        }

        public List<ImageMessageData> LoadCampaigns()
        {
            var path = Path.Combine(Application.persistentDataPath, STORAGE_PATH);
            if (!File.Exists(path))
                return new List<ImageMessageData>();

            var json = File.ReadAllText(path);
            var data = JsonConvert.DeserializeObject<StoredCampaigns>(json);
            var campaigns = data?.Campaigns ?? new List<ImageMessageData>();

            // Cleanup expired campaigns
            var cleaned = CleanupExpiredAndReturnActive(campaigns);
            if (cleaned.Count != campaigns.Count)
            {
                // Save updated list if we removed expired campaigns
                SaveCampaigns(cleaned);
            }

            return cleaned;
        }

        /// <summary>
        /// Remove expired campaigns and delete their cached media files.
        /// A campaign is considered expired if it has CampaignDurationInHours and the received time + duration is in the past.
        /// </summary>
        private List<ImageMessageData> CleanupExpiredAndReturnActive(List<ImageMessageData> loaded)
        {
            var result = new List<ImageMessageData>();
            var mediaFolder = Path.Combine(Application.persistentDataPath, CAMPAIGN_MEDIA_FOLDER);
            var now = DateTime.UtcNow;

            foreach (var c in loaded)
            {
                var md = c.Metadata;
                if (!md.CampaignDurationInHours.HasValue)
                {
                    // no expiration defined -> keep
                    result.Add(c);
                    continue;
                }

                // Determine the reference start time
                DateTime? startUtc = md.ReceivedAtUtc;

                // if ReceivedAtUtc not present, try to infer from file creation time (image file)
                if (!startUtc.HasValue)
                {
                    var imagePath = md.ImageUrl;
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        // if saved path is relative to media folder, combine
                        if (!Path.IsPathRooted(imagePath))
                            imagePath = Path.Combine(mediaFolder, imagePath);

                        if (File.Exists(imagePath))
                        {
                            startUtc = File.GetCreationTimeUtc(imagePath);
                        }
                    }
                }

                if (!startUtc.HasValue)
                {
                    // cannot determine start time -> treat as expired (safe cleanup)
                    DeleteCampaignFilesIfExist(md);
                    continue;
                }

                var expireAt = startUtc.Value.AddHours(md.CampaignDurationInHours.Value);
                if (now > expireAt)
                {
                    // expired -> delete files and skip
                    DeleteCampaignFilesIfExist(md);
                    continue;
                }

                // still active -> keep
                result.Add(c);
            }

            return result;
        }

        private void DeleteCampaignFilesIfExist(Metadata md)
        {
            try
            {
                var mediaFolder = Path.Combine(Application.persistentDataPath, CAMPAIGN_MEDIA_FOLDER);

                if (!string.IsNullOrEmpty(md.ImageUrl))
                {
                    var imagePath = md.ImageUrl;
                    if (!Path.IsPathRooted(imagePath))
                        imagePath = Path.Combine(mediaFolder, imagePath);

                    if (File.Exists(imagePath))
                        File.Delete(imagePath);
                }

                if (!string.IsNullOrEmpty(md.IconUrl))
                {
                    var iconPath = md.IconUrl;
                    if (!Path.IsPathRooted(iconPath))
                        iconPath = Path.Combine(mediaFolder, iconPath);

                    if (File.Exists(iconPath))
                        File.Delete(iconPath);
                }
            }
            catch (Exception ex)
            {
                Utils.MocaLibLogWarning(TAG, $"Failed to delete campaign files: {ex}");
            }
        }

        private IEnumerator DownloadAndCacheCampaignMediaCoroutine(ImageMessageData campaign)
        {
            var campaignNameSafe = MakeSafeFileName(campaign.Metadata.CampaignName);
            var mediaFolder = Path.Combine(Application.persistentDataPath, CAMPAIGN_MEDIA_FOLDER);
            Directory.CreateDirectory(mediaFolder);

            // Helper to download a single image (url) to local file name
            IEnumerator DownloadOne(string url, string localFileName, Action<string> onCompleted)
            {
                if (string.IsNullOrEmpty(url))
                {
                    onCompleted?.Invoke(null);
                    yield break;
                }

                var localPath = Path.Combine(mediaFolder, localFileName);

                // If local file already exists, return it immediately
                if (File.Exists(localPath))
                {
                    onCompleted?.Invoke(localPath);
                    yield break;
                }

                using var request = UnityWebRequestTexture.GetTexture(url);
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Utils.MocaLibLogWarning(TAG, $"Failed to download {url}: {request.error}");
                    onCompleted?.Invoke(null);
                    yield break;
                }

                var tex = DownloadHandlerTexture.GetContent(request);
                try
                {
                    var bytes = tex.EncodeToPNG();
                    File.WriteAllBytes(localPath, bytes);
                    onCompleted?.Invoke(localPath);
                }
                catch (Exception ex)
                {
                    Utils.MocaLibLogWarning(TAG, $"Failed to save image to {localPath}: {ex}");
                    onCompleted?.Invoke(null);
                }
            }

            string localImagePath = null;
            string localIconPath = null;

            var imageFileName = $"{campaignNameSafe}_image.png";
            var iconFileName = $"{campaignNameSafe}_icon.png";

            // Download image
            yield return StartCoroutine(DownloadOne(campaign.Metadata.ImageUrl, imageFileName, p => localImagePath = p));
            // Download icon
            yield return StartCoroutine(DownloadOne(campaign.Metadata.IconUrl, iconFileName, p => localIconPath = p));

            // If we have at least one local file, update stored campaigns to reference local paths
            if (!string.IsNullOrEmpty(localImagePath) || !string.IsNullOrEmpty(localIconPath))
            {
                try
                {
                    var saved = LoadCampaigns();
                    var idx = saved.FindIndex(x => x.Metadata.CampaignName == campaign.Metadata.CampaignName);
                    if (idx >= 0)
                    {
                        var updated = saved[idx];
                        if (!string.IsNullOrEmpty(localImagePath))
                            updated.Metadata.ImageUrl = localImagePath;
                        if (!string.IsNullOrEmpty(localIconPath))
                            updated.Metadata.IconUrl = localIconPath;
                        // ensure ReceivedAtUtc exists
                        if (!updated.Metadata.ReceivedAtUtc.HasValue)
                            updated.Metadata.ReceivedAtUtc = DateTime.UtcNow;

                        saved[idx] = updated;
                        SaveCampaigns(saved);
                    }
                }
                catch (Exception ex)
                {
                    Utils.MocaLibLogWarning(TAG, $"Failed to update saved campaign paths: {ex}");
                }
            }
        }

        private string MakeSafeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return Guid.NewGuid().ToString("N");
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }
}
