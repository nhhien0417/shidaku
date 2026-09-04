using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace StorageUtils.Utils
{
    public static class SerializeUtils
    {
        public static string SerializeObject<T>(T data)
        {
            if (data is IEnumerable<ICustomSerializableData> list)
            {
                foreach (var d in list)
                {
                    d.OnPreSerialize();
                }
            }
            else if (data is ICustomSerializableData customData)
            {
                customData.OnPreSerialize();
            }
            return JsonConvert.SerializeObject(data);
        }
    
        public static T DeserializeObject<T>(string json)
        {
            var data = JsonConvert.DeserializeObject<T>(json);
            if (data is IEnumerable<ICustomSerializableData> list)
            {
                foreach (var d in list)
                {
                    d.OnPostDeserialize();
                }
            }
            else if (data is ICustomSerializableData customData)
            {
                customData.OnPostDeserialize();
            }

            return data;
        }
    }
}