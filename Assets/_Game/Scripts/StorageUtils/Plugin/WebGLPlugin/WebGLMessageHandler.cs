using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WebGLMessageHandler : MonoBehaviour
{
    private Action<string[]> onFileReceived;
    
    public void RegisterFileReceivedCallback(Action<string[]> callback)
    {
        UnregisterFileReceivedCallback(callback);
        onFileReceived += callback;
    }
    
    public void UnregisterFileReceivedCallback(Action<string[]> callback)
    {
        onFileReceived -= callback;
    }
    
    public void HandleFile(string fileUrl) // This function will be called from the browser via JavaScript using SendMessage logic
    {
        StartCoroutine(LoadBlob(fileUrl));
    }
    
    public void HandleData(string data) // This function will be called from the browser via JavaScript using SendMessage logic
    {
        if (string.IsNullOrEmpty(data))
            data = "";
        var result = data.Split(WebGLPluginJS.DATA_SEPARATOR);
        onFileReceived?.Invoke(result);
    }

    private IEnumerator LoadBlob(string url)
    {
        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();
        
        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            var rawData = webRequest.downloadHandler.text;
            Debug.Log(rawData);
            
            var data = rawData.Split(WebGLPluginJS.DATA_SEPARATOR);
            onFileReceived?.Invoke(data);
        }
        else
        {
            Debug.LogError($"Failed to load file from url: {url} with error: {webRequest.error}");
        }
    }
}
