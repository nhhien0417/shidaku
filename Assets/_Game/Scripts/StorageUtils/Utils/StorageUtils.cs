using System;
using System.IO;
using UnityEngine;

namespace StorageUtils.Utils
{
    public static class StorageUtils
    {
        public static void DownloadToFile(string data, string fileName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLPluginJS.DownloadToFile(data, fileName);
            
#elif UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
            var path = Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop)+"\\"+Application.productName;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            File.WriteAllText(path+"\\"+fileName, data);
			
    #if UNITY_STANDALONE_OSX
			FileUtils.OpenInMacFileBrowser(path);
    #else
            Application.OpenURL(path);
    #endif
#endif
        }
    }
}