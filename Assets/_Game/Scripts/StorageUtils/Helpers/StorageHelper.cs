using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using CodeStage.AntiCheat.Storage;
using UnityEngine;

namespace StorageUtils.Helpers
{
    public class StorageHelper 
    {
        public static T Load<T>(string fileName) where T : class
        {
            var filepath = GetFilePath(fileName);
            if (File.Exists(filepath))
            {
                using (FileStream file = File.Open(filepath, FileMode.Open))
                {
                    object loadedData = new BinaryFormatter().Deserialize(file);
                    var data = (T)loadedData;
                    return data;
                }
            }

            return default(T);
        }

        public static void Save<T>(T data, string fileName) where T : class
        {
            var filepath = GetFilePath(fileName);
            using (FileStream file = File.Create(filepath))
            {
                new BinaryFormatter().Serialize(file, data);
            }
        }

        public static void DeleteSave(string fileName)
        {
            var filepath = GetFilePath(fileName);
            if (File.Exists(filepath))
            {
                File.Delete(filepath);
            }
        }

        public static T LoadObscured<T>(string fileName) where T : class
        {
            var obscuredFilepath = GetObscuredFilePath(fileName);
            var safeFile = new ObscuredFile(obscuredFilepath, new ObscuredFileSettings(new EncryptionSettings("G!WDM#4Q&BF@Z*WQ(UPU%3I9VQU^30^HE8RD&27N"), new DeviceLockSettings(), ObscuredFileLocation.Custom));
            if (safeFile.FileExists)
            {
                var result = safeFile.ReadAllBytes();
                if (result.Success)
                {
                    using (var stream = new MemoryStream(result.Data))
                    {
                        object loadedData = new BinaryFormatter().Deserialize(stream);
                        var data = (T)loadedData;
                        return data;
                    }
                }
                else
                {
                    Debug.LogError($"Load file failed: {result.Error}");
                }
            }

            return default(T);
        }

        public static void SaveObscured<T>(T data, string fileName) where T : class
        {
            var obscuredFilepath = GetObscuredFilePath(fileName);
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, data);
                var bytes = stream.ToArray();
                var safeFile = new ObscuredFile(obscuredFilepath, new ObscuredFileSettings(new EncryptionSettings("G!WDM#4Q&BF@Z*WQ(UPU%3I9VQU^30^HE8RD&27N"), new DeviceLockSettings(), ObscuredFileLocation.Custom));
                var result = safeFile.WriteAllBytes(bytes);

                if (!result.Success)
                {
                    Debug.LogError($"Save file failed: {result.Error}");
                }
            }
        }

        public static void DeleteObscuredSave(string fileName)
        {
            var obscuredFilepath = GetObscuredFilePath(fileName);
            if (File.Exists(obscuredFilepath))
            {
                File.Delete(obscuredFilepath);
            }
        }
        
        public static void CreateDirectory(string directoryName)
        {
            var dirPath = Application.persistentDataPath + "/" + directoryName;
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }
        
        private static string GetFilePath(string fileName)
        {
            return Application.persistentDataPath + "/" + fileName;
        }
        
        private static string GetObscuredFilePath(string fileName)
        {
            return Application.persistentDataPath + "/obs_" + fileName;
        }
    }
}
