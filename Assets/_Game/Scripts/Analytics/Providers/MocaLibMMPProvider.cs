using System.Collections.Generic;
using Titipi.MocaLib.Runtime.Services;
using UnityEngine;

namespace Analytics.Providers
{
    /// <summary>
    /// Analytics provider for MMP (Mobile Measurement Partner) events via MocaLib.
    /// Routes events to MocaLib.Instance.MMPManager (e.g., AppsFlyer).
    ///
    /// Only logs specific MMP events - not all analytics events are relevant for MMP.
    /// Override ShouldLogEvent() to control which events are forwarded.
    /// </summary>
    public class MocaLibMMPProvider : IAnalyticsProvider
    {
        public string ProviderName => "MocaLib_MMP";

        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Mapping from analytics event names to MMP event names.
        /// Only events in this map will be forwarded to MMP.
        /// </summary>
        private static readonly Dictionary<string, string> EventNameMapping = new()
        {
            { EventNames.AdRewardRequest, "af_rewarded_logicgame" },
            { EventNames.AdInterRequest, "af_inters_logicgame" },
        };

        public void Initialize()
        {
            Debug.Log("[MocaLibMMPProvider] Initialized.");
        }

        public void LogEvent(AnalyticsEvent analyticsEvent)
        {
            if (EventNameMapping.TryGetValue(analyticsEvent.EventName, out var mmpEventName))
            {
                MocaLib.Instance.MMPManager.LogEvent(mmpEventName);
            }
        }

        public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (EventNameMapping.TryGetValue(eventName, out var mmpEventName))
            {
                MocaLib.Instance.MMPManager.LogEvent(mmpEventName);
            }
        }

        /// <summary>
        /// Log a direct MMP event by its AppsFlyer event name.
        /// Use this for MMP-specific events that don't map to analytics events.
        /// </summary>
        public void LogMMPEvent(string mmpEventName)
        {
            MocaLib.Instance.MMPManager.LogEvent(mmpEventName);
        }
    }
}
