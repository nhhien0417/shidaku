using System;
using System.Collections;
using System.Collections.Generic;
using StorageUtils.Utils;
using UnityEngine;

namespace StorageUtils
{
    public class DataHolder<T> where T : IHasId
    {
        protected string _filePath;
        protected string _folderPath;
        
        public Dictionary<string, T> AllData { get; protected set; }
        public T FirstData { get; protected set; }
        public T LastestData { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath">Path of json file that contains config for multi data-with-id.</param>
        /// <param name="folderPath">Path of folder that contains all json data_file. Each data_file contains config for single data-with-id and must have name as id.</param>
        public DataHolder(string filePath, string folderPath)
        {
            _filePath = filePath;
            _folderPath = folderPath;
            AllData = new();
        }
        
        public virtual void LoadDesign()
        {
            if (string.IsNullOrEmpty(_filePath))
                return;
            
            var textAsset = Resources.Load<TextAsset>(_filePath);
            if (textAsset != null)
            {
                LoadDataDesign(textAsset.text);
            }
        }
        
        protected void LoadDataDesign(string json)
        {
            AllData = new();
            try
            {
                var data = SerializeUtils.DeserializeObject<List<T>>(json);
                if (data is {Count: > 0})
                {
                    foreach (var d in data)
                    {
                        AllData.Add(d.GetId(), d);
                    }
                    FirstData = data[0];
                    LastestData = data[^1];
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[{_filePath}]LoadDataDesign: {e.Message}");
            }
        }
        
        public T GetData(string id)
        {
            if (AllData.TryGetValue(id, out var data))
            {
                return (T)data;
            }
            
            data = LoadData(id);
            if (data != null)
            {
                if (!AllData.TryAdd(id, data))
                {
                    Debug.LogError($"Duplicate data id: {id}");
                }
            }
            
            return (T)data;
        }

        public virtual void SetData(params T[] data)
        {
            foreach (var d in data)
            {
                var id = d.GetId();
                AllData[id] = d;
            }
        }

        private T LoadData(string id)
        {
            var path = $"{_folderPath}/{id}";
            var data = Resources.Load<TextAsset>($"{path}");

            if (data != null && !string.IsNullOrEmpty(data.text))
            {
                try
                {
                    var result = SerializeUtils.DeserializeObject<T>(data.text);
                    return result;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[{path}]LoadData: {e.Message}");
                }
            }
            
            return default;
        }
        
        #if UNITY_EDITOR
        public void ExportToFile(string dataPath)
        {
            if (string.IsNullOrEmpty(_filePath))
                return;
            
            var json = SerializeUtils.SerializeObject(AllData.Values);
            var fullPath = $"{dataPath}/{_filePath}.json";
            System.IO.File.WriteAllText(fullPath, json);
            Debug.Log($"Export data to: {fullPath}");
        }
        #endif
    }
}