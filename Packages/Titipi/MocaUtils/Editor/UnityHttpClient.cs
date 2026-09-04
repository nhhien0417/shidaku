using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Titipi.Editor
{
    public static class UnityHttpClient
    {
        public static async Task<string> GetAsync(string url, Dictionary<string, string> headers = null)
        {
            using var request = UnityWebRequest.Get(url);
            AddHeaders(request, headers);
            return await SendAsync(request);
        }

        public static async Task<string> PostAsync(string url, string json, Dictionary<string, string> headers = null)
        {
            using var request = new UnityWebRequest(url, "POST");
            SetupRequestWithJson(request, json);
            AddHeaders(request, headers);
            return await SendAsync(request);
        }

        public static async Task<string> PutAsync(string url, string json, Dictionary<string, string> headers = null)
        {
            using var request = new UnityWebRequest(url, "PUT");
            SetupRequestWithJson(request, json);
            AddHeaders(request, headers);
            return await SendAsync(request);
        }

        private static void SetupRequestWithJson(UnityWebRequest request, string json)
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
        }

        private static void AddHeaders(UnityWebRequest request, Dictionary<string, string> headers)
        {
            if (headers == null) return;

            foreach (var kvp in headers)
            {
                request.SetRequestHeader(kvp.Key, kvp.Value);
            }
        }

        private static async Task<string> SendAsync(UnityWebRequest request)
        {
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                return request.downloadHandler.text;
            }

            Debug.LogError($"HTTP Error {request.responseCode}: {request.error}");
            return null;
        }
    }
}
