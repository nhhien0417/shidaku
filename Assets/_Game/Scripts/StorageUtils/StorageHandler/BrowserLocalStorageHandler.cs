using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace StorageUtils.StorageHandler
{
    public class BrowserLocalStorageHandler<T> : IStorageHandler<T>
    {
        public string DataFolder { get; private set; }
        
        public BrowserLocalStorageHandler(string dataFolder)
        {
            DataFolder = dataFolder;
        }
        
        public Task<string> SaveData(T data, string id)
        {
#if UNITY_WEBGL
            var path = GetFullPath(id);
            var content = JsonConvert.SerializeObject(data);
            
            var saveSuccess = WebGLPluginJS.SaveToLocalStorage(path, content);
            if (!saveSuccess)
                return Task.FromResult(ErrorMessage.OutOfMemory);
#endif
            
            return Task.FromResult("");
        }

        public Task<T> GetData(string id)
        {
#if UNITY_WEBGL
            var path = GetFullPath(id);
            var fileData = WebGLPluginJS.LoadFromLocalStorage(path);
            if (string.IsNullOrEmpty(fileData))
                return Task.FromResult(default(T));
            
            return Task.FromResult(JsonConvert.DeserializeObject<T>(fileData));
#else
            return Task.FromResult(default(T));
#endif
        }

        public Task<List<T>> GetAllData()
        {
#if UNITY_WEBGL
            var files = WebGLPluginJS.GetAllLocalStorageKeysAsList(GetFolderPath());
            
            var data = new List<T>();
            foreach (var file in files)
            {
                var fileData = WebGLPluginJS.LoadFromLocalStorage(file);
                if (string.IsNullOrEmpty(fileData))
                    continue;
                
                data.Add(JsonConvert.DeserializeObject<T>(fileData));
            }
            
            return Task.FromResult(data);
#else
            return Task.FromResult(new List<T>());
#endif
        }
        
        public void ShowStorageLocation()
        {
#if UNITY_WEBGL
            WebGLPluginJS.Alert("Cannot show storage location in browser");
#endif
        }

        private string GetFullPath(string fileName)
        {
            return $"{GetFolderPath()}{fileName}";
        }
        
        private string GetFileNameFromPath(string path)
        {
            var folderPath = GetFolderPath();
            return path.Replace(folderPath, "");
        }

        private string GetFolderPath()
        {
            return string.IsNullOrEmpty(DataFolder) ? "" : $"[{DataFolder}]";
        }
    }
}