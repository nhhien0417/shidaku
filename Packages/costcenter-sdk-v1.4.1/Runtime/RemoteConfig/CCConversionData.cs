using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace CostCenter.RemoteConfig
{
    [System.Serializable]
    public class StringStringPair {
        public string key;
        public string value;
    }

    [System.Serializable]
    public class StringStringDictionary {
        public List<StringStringPair> items = new List<StringStringPair>();
    }

    public class CCConversionData
    {
        public const string PREFS_KEY = "CC_ConversionData";
        public static readonly string[] CONVERSION_FIELDS = new string[] {
            "media_source",
            "install_time",
            "af_siteid",
            "adgroup_id",
            "adset",
            "adset_id",
            "campaign_id",
            "campaign"
        };

        // Save Dictionary
        public static void Save(Dictionary<string, object> dict)
        {
            try
            {
                StringStringDictionary wrapper = new StringStringDictionary();
                foreach (var kvp in dict)
                {
                    if (
                        kvp.Value != null
                        && kvp.Value is string
                        && !string.IsNullOrEmpty(kvp.Value.ToString())
                        && !string.IsNullOrEmpty(kvp.Key)
                        && CONVERSION_FIELDS.Contains(kvp.Key)
                    )
                    {
                        wrapper.items.Add(new StringStringPair { key = kvp.Key, value = kvp.Value.ToString() });
                    }
                }
                string json = JsonUtility.ToJson(wrapper);
                PlayerPrefs.SetString(PREFS_KEY, json);
                PlayerPrefs.Save();
            } catch (System.Exception e) {
                Debug.LogError($"[SaveDictionary] Failed to save dictionary: {e.Message}");
            }
        }

        // Load Dictionary
        public static Dictionary<string, object> Load() {
            var dict = new Dictionary<string, object>();
            string json = PlayerPrefs.GetString(PREFS_KEY, "{}");
            try {
                StringStringDictionary wrapper = JsonUtility.FromJson<StringStringDictionary>(json);
                foreach (var pair in wrapper.items) {
                    dict[pair.key] = pair.value;
                }
            } catch (System.Exception e) {
                Debug.LogWarning($"[LoadDictionary] Failed to parse JSON for key {PREFS_KEY}: {e.Message}");
            }
            return dict;
        }
    }
}
