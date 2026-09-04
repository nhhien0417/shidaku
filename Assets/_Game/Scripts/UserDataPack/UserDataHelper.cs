using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using CodeStage.AntiCheat.Storage;
using UnityEngine;

namespace UserDataPack
{
    public class UserDataHelper
    {
        private static readonly string filepath = Application.persistentDataPath + "/usr_v2.bin";
        private static readonly string obscuredFilepath = Application.persistentDataPath + "/usrobs_v2.bin";

        public static T Load<T>() where T : UserData
        {
            if (File.Exists(filepath))
            {
                try
                {
                    using (FileStream file = File.Open(filepath, FileMode.Open))
                    {
                        object loadedData = new BinaryFormatter().Deserialize(file);
                        var data = (T)loadedData;
                        return data;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Load file failed: {e}");
                }
            }

            return default(T);
        }

        public static void Save<T>(T data) where T : UserData
        {
            using (FileStream file = File.Create(filepath))
            {
                new BinaryFormatter().Serialize(file, data);
            }
        }

        public static void DeleteSave()
        {
            if (File.Exists(filepath))
            {
                File.Delete(filepath);
            }
        }

        public static T LoadObscured<T>() where T : UserData
        {
            var safeFile = new ObscuredFile(obscuredFilepath, new ObscuredFileSettings(new EncryptionSettings("G!WDM#4Q&BF@Z*WQ(UPU%3I9VQU^30^HE8RD&27N"), new DeviceLockSettings(), ObscuredFileLocation.Custom));
            if (safeFile.FileExists)
            {
                var result = safeFile.ReadAllBytes();
                if (result.Success)
                {
                    try
                    {
                        using (var stream = new MemoryStream(result.Data))
                        {
                            object loadedData = new BinaryFormatter().Deserialize(stream);
                            var data = (T)loadedData;
                            return data;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Load file failed: {e}");
                    }
                }
                else
                {
                    Debug.LogError($"Load file failed: {result.Error}");
                }
            }

            return default(T);
        }

        public static void SaveObscured<T>(T data) where T : UserData
        {
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

        public static void DeleteObscuredSave()
        {
            // No! Never do this!
        }
    }
}