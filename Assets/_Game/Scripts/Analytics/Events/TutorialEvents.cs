using System.Collections.Generic;

namespace Analytics.Events
{
    public class TutorialStepEvent : AnalyticsEvent
    {
        public override string EventName => EventNames.TutorialStep;

        public string TutorialId { get; set; }
        public int StepId { get; set; }
        public string StepName { get; set; }
        public string Status { get; set; }
        public float CompleteTime { get; set; }

        public override Dictionary<string, object> ToParameters()
        {
            var parameters = new Dictionary<string, object>
            {
                { "tutorial_id", TutorialId },
                { "step_id", StepId },
                { "step_name", StepName },
                { "status", Status }
            };

            if (Status == TutorialStatus.Complete)
            {
                parameters["complete_time"] = CompleteTime;
            }

            return parameters;
        }
    }
}
