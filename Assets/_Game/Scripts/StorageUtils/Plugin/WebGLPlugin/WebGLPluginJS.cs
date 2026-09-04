using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class WebGLPluginJS
{
    public const string DATA_SEPARATOR = "////////////////////////%$^*&#//////////////////////$)($&%)($*%)$)%_/////////$)%*(&@!(*$&%//////////////";
    
#if UNITY_WEBGL
    [DllImport("__Internal")]
    public static extern void Alert(string message);
    
    [DllImport("__Internal")]
    public static extern string Prompt(string message);

    [DllImport("__Internal")]
    public static extern void DownloadToFile(string content, string filename);
    
    [DllImport("__Internal")]
    public static extern void DownloadFile(byte[] array, int byteLength, string fileName);
    
    [DllImport("__Internal")]
    public static extern void UploadFile();
    
    [DllImport("__Internal")]
    public static extern bool SaveToLocalStorage(string key, string value);

    [DllImport("__Internal")]
    public static extern string LoadFromLocalStorage(string key);

    [DllImport("__Internal")]
    public static extern void RemoveFromLocalStorage(string key);

    [DllImport("__Internal")]
    public static extern int HasKeyInLocalStorage(string key);
    
    [DllImport("__Internal")]
    public static extern string GetAllLocalStorageKeys(string prefix, string separation);
#endif
    
    public static void UploadFile(Action<string[]> callback)
    {
#if UNITY_WEBGL
        GameObject.FindAnyObjectByType<WebGLMessageHandler>()?.RegisterFileReceivedCallback(callback);
        UploadFile();
#endif
    }

    public static List<string> GetAllLocalStorageKeysAsList(string prefix)
    {
#if UNITY_WEBGL
        var data = GetAllLocalStorageKeys(prefix, "/");
        if (string.IsNullOrEmpty(data))
        {
            return new List<string>();
        }
        
        return new List<string>(data.Split('/'));
#else
        return new List<string>();
#endif
    }

    public static void DownloadData(string[] data, string fileName)
    {
#if UNITY_WEBGL
        var content = string.Join(DATA_SEPARATOR, data);
        DownloadToFile(content, fileName);
#endif
    }
}
