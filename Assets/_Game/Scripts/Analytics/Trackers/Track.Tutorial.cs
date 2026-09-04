using Analytics;
using Analytics.Events;
using UnityEngine;

public static partial class Track
{
    public static class Tutorial
    {
        private static float _stepStartTime;

        public static void StepStart(string tutorialId, int stepId, string stepName)
        {
            _stepStartTime = Time.realtimeSinceStartup;

            AnalyticsManager.Instance.Track(new TutorialStepEvent
            {
                TutorialId = tutorialId,
                StepId = stepId,
                StepName = stepName,
                Status = TutorialStatus.Start
            });
        }

        public static void StepComplete(string tutorialId, int stepId, string stepName)
        {
            AnalyticsManager.Instance.Track(new TutorialStepEvent
            {
                TutorialId = tutorialId,
                StepId = stepId,
                StepName = stepName,
                Status = TutorialStatus.Complete,
                CompleteTime = Time.realtimeSinceStartup - _stepStartTime
            });
        }
    }
}
