using System;
using System.Collections.Generic;
using UnityEngine;

namespace CostCenter.RemoteConfig {
    [Serializable]
    public class CCConversionConfig
    {
        public string campaign;
        public string campaign_id;
        public string adset;
        public string adset_id;
        public string adgroup_id;
        public string media_source;
        public string install_time;
        public string af_siteid;
        public object value;
        
        public bool IsMapWithConversionData(Dictionary<string, object> conversionData)
        {
            if (conversionData == null || conversionData.Count < 1)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(campaign) || !string.IsNullOrEmpty(campaign_id))
            {
                object conversionCampaign = conversionData.GetValueOrDefault("campaign", null);
                object conversionCampaignId = conversionData.GetValueOrDefault("campaign_id", null);
                if (
                    (
                        (string.IsNullOrEmpty(campaign) && (conversionCampaign == null || string.IsNullOrEmpty(conversionCampaign.ToString())))
                        || (campaign != null && !campaign.Equals(conversionCampaign))
                    )
                    && (
                        (string.IsNullOrEmpty(campaign_id) && (conversionCampaignId == null || string.IsNullOrEmpty(conversionCampaignId.ToString())))
                        || (campaign_id != null && !campaign_id.Equals(conversionCampaignId))
                    )
                )
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(adset) || !string.IsNullOrEmpty(adset_id))
            {
                object conversionAdset = conversionData.GetValueOrDefault("adset", null);
                object conversionAdsetId = conversionData.GetValueOrDefault("adset_id", null);
                if (
                    (
                        (string.IsNullOrEmpty(adset) && (conversionAdset == null || string.IsNullOrEmpty(conversionAdset.ToString())))
                        || (adset != null && !adset.Equals(conversionAdset))
                    )
                    && (
                        (string.IsNullOrEmpty(adset_id) && (conversionAdsetId == null || string.IsNullOrEmpty(conversionAdsetId.ToString())))
                        || (adset_id != null && !adset_id.Equals(conversionAdsetId))
                    )
                )
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(adgroup_id))
            {
                if (!adgroup_id.Equals(conversionData.GetValueOrDefault("adgroup_id", string.Empty)))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(media_source))
            {
                if (!media_source.Equals(conversionData.GetValueOrDefault("media_source", string.Empty)))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(install_time))
            {
                if (!install_time.Equals(conversionData.GetValueOrDefault("install_time", string.Empty)))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(af_siteid))
            {
                if (!af_siteid.Equals(conversionData.GetValueOrDefault("af_siteid", string.Empty)))
                {
                    return false;
                }
            }

            return (
                !string.IsNullOrEmpty(campaign)
                || !string.IsNullOrEmpty(campaign_id)
                || !string.IsNullOrEmpty(adset)
                || !string.IsNullOrEmpty(adset_id)
                || !string.IsNullOrEmpty(adgroup_id)
                || !string.IsNullOrEmpty(media_source)
                || !string.IsNullOrEmpty(install_time)
                || !string.IsNullOrEmpty(af_siteid)
            );
        }
    }
}
