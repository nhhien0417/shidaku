
using System.Collections.Generic;
using Analytics;
using Analytics.Events;

public static partial class Track
{
    public static void CustomEvent(string eventId, Dictionary<string, object> parameters)
    {
        AnalyticsManager.Instance.Track(new CustomEvents(eventId, parameters));
    }
}