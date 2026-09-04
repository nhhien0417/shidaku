using System.Collections.Generic;

namespace Analytics
{
    public abstract class AnalyticsEvent
    {
        public abstract string EventName { get; }

        public abstract Dictionary<string, object> ToParameters();

        public override string ToString()
        {
            var parameters = ToParameters();
            var sb = new System.Text.StringBuilder();
            sb.Append($"[{EventName}] ");
            foreach (var kvp in parameters)
            {
                sb.Append($"{kvp.Key}={kvp.Value}, ");
            }
            return sb.ToString().TrimEnd(',', ' ');
        }
    }
}
