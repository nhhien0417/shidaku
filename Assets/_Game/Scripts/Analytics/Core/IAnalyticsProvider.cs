using System.Collections.Generic;

namespace Analytics
{
    public interface IAnalyticsProvider
    {
        string ProviderName { get; }
        bool IsEnabled { get; set; }

        void Initialize();
        void LogEvent(AnalyticsEvent analyticsEvent);
        void LogEvent(string eventName, Dictionary<string, object> parameters = null);
    }
}
