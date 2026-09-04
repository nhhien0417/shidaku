using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace RemoteConfigs
{
    [Serializable]
    public class PlaytimeGroupConfig : GroupConfig<GroupDataMinMax<long, int>, long, int>
    {
        public override void ApplyRemoteConfig(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                var jsonData = JSON.Parse(json);
                if (jsonData == null || !jsonData.IsArray) return;

                var groups = new List<GroupDataMinMax<long, int>>();
                var startIndex = 0;
                foreach (var group in jsonData.AsArray)
                {
                    if (group.Value.IsArray && group.Value.Count == 2)
                    {
                        groups.Add(new ()
                        {
                            Min = group.Value[0].AsLong,
                            Max = group.Value[1].AsLong,
                            GroupId = startIndex,
                        });
                    }
                    else
                    {
                        Debug.LogWarning($"[PlaytimeGroupConfig] Invalid group format: {group.Value}");
                    }

                    startIndex++;
                }

                Groups = groups;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlaytimeGroupConfig] Failed to apply remote config: {e.Message}");
            }
        }

        public override int GetGroupId(long playtime)
        {
            if (playtime <= 0 || Groups.Count == 0)
                return 0;

            foreach (var g in Groups)
            {
                if (g.IsMatch(playtime))
                    return g.GetGroupId();
            }

            return Groups[^1].GetGroupId();
        }
    }
}