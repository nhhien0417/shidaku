using System;
using System.Collections;
using System.Collections.Generic;
using MEC;
using UnityEngine;
using UnityEngine.Networking;

namespace DesignTool.Utils
{
    public class DownloadGoogleDriveDataWithUrl
    {
        private static string downloadLink = "https://drive.google.com/uc?export=download&id={0}";

        public static void GetData(string url, Action<string> onComplete)
        {
            if (!url.Contains(downloadLink))
            {
                var elm = url.Split("/");
                if (elm.Length < 5)
                {
                    onComplete?.Invoke("");
                    return;
                }
                url = string.Format(downloadLink, elm[5]);
            }
            
            Timing.RunCoroutine(DownloadData(url, onComplete));
        }
        
        private static IEnumerator<float> DownloadData(string url, Action<string> onComplete)
        {
            var webRequest = UnityWebRequest.Get(url);
            var operation = webRequest.SendWebRequest();

            while (!operation.isDone)
            {
                yield return Timing.WaitForOneFrame;
            }

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke("");
                yield break;
            }
            
            onComplete?.Invoke(webRequest.downloadHandler.text);
        }
    }
}
