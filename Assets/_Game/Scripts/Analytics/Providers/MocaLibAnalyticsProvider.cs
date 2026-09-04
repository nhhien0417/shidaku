using System.Collections.Generic;
using Titipi.MocaLib.Runtime.Services;
using UnityEngine;

namespace Analytics.Providers
{
    /// <summary>
    /// Analytics provider that wraps the existing MocaLib AnalyticsManager.
    /// Routes all typed and raw events to MocaLib.Instance.AnalyticsManager.LogEvent().
    /// </summary>
    public class MocaLibAnalyticsProvider : IAnalyticsProvider
    {
        public string ProviderName => "MocaLib";

        public bool IsEnabled { get; set; } = true;

        public void Initialize()
        {
            Debug.Log("[MocaLibAnalyticsProvider] Initialized.");
        }

        public void LogEvent(AnalyticsEvent analyticsEvent)
        {
            var parameters = analyticsEvent.ToParameters();
            MocaLib.Instance.AnalyticsManager.LogEvent(analyticsEvent.EventName, parameters);
        }

        public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (parameters != null)
            {
                MocaLib.Instance.AnalyticsManager.LogEvent(eventName, parameters);
            }
            else
            {
                MocaLib.Instance.AnalyticsManager.LogEvent(eventName);
            }
        }
    }
}
