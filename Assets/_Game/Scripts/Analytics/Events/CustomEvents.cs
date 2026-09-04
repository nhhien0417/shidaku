using System.Collections.Generic;

namespace Analytics.Events
{
    public class CustomEvents : AnalyticsEvent
    {
        public override string EventName => _eventName;

        private string _eventName;
        private Dictionary<string, object> _parameters;

        public CustomEvents(string eventName, Dictionary<string, object> parameters)
        {
            _eventName = eventName;
            _parameters = parameters;
        }

        public override Dictionary<string, object> ToParameters()
        {
            return _parameters;
        }
    }
}