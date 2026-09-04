using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using StorageUtils.Utils;
using UnityEngine;

namespace StorageUtils.StorageHandler
{
    public class LocalStorageHandler<T> : IStorageHandler<T>
    {
        private string _folderPath;
        private string _filepath;
        private string _dataFolder;

        public LocalStorageHandler(string dataFolder)
        {
	        _dataFolder = dataFolder;
	        _folderPath = Application.persistentDataPath + "/LocalStorage/" + dataFolder;
	        _filepath = _folderPath + "/{0}.json";
        }
        
        public Task<string> SaveData(T data, string id)
        {
            if(!Directory.Exists(_folderPath))
                 Directory.CreateDirectory(_folderPath);
             
            var path = string.Format(_filepath, id);
            File.WriteAllText(path, SerializeUtils.SerializeObject(data));
            
            return Task.FromResult("");
        }

        public Task<T> GetData(string id)
        {
            var result = default(T);
            var path = string.Format(_filepath, id);
            
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);

                try
                {
                    result = SerializeUtils.DeserializeObject<T>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to deserialize data: {e.Message}");
                    result = default(T);
                }
            }

            return Task.FromResult(result);
        }

        public Task<List<T>> GetAllData()
        {
			var result = new List<T>();
			
			if(Directory.Exists(_folderPath))
			{
				var files = Directory.GetFiles(_folderPath);
				foreach (var file in files)
				{
					var json = File.ReadAllText(file);
					try
					{
						result.Add(SerializeUtils.DeserializeObject<T>(json));
					}
					catch (Exception e)
					{
						Debug.LogError($"Failed to deserialize data: {e.Message}");
					}
				}
			}
			
			return Task.FromResult(result);
        }
        
        public void ShowStorageLocation()
        {
#if UNITY_STANDALONE_OSX
			FileUtils.OpenInMacFileBrowser(_folderPath);
#else
	        Application.OpenURL(_folderPath);
#endif
        }
    }
}
