using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class WebGLPluginDesignParser
{
#if UNITY_WEBGL
    [DllImport("__Internal")]
    public static extern string ParseDesignFromFileUpload(string separator);
    
    [DllImport("__Internal")]
    public static extern string ParseDesignFromDrawIOData(string data);
#endif
    
    public static void ParseDesignFromFileUpload(Action<string[]> callback)
    {
#if UNITY_WEBGL
        GameObject.FindAnyObjectByType<WebGLMessageHandler>()?.RegisterFileReceivedCallback(callback);
        ParseDesignFromFileUpload(WebGLPluginJS.DATA_SEPARATOR);
#endif
    }
}
