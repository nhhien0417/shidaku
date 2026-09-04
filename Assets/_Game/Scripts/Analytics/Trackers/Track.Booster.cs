using Analytics;
using Analytics.Events;

public static partial class Track
{
    public static class Booster
    {
        public static void Spend(int level, string playMode, string puzzleId, string boosterName)
        {
            AnalyticsManager.Instance.Track(new BoosterSpendEvent
            {
                Level = level,
                PlayMode = playMode,
                PuzzleId = puzzleId,
                BoosterName = boosterName
            });
        }

        public static void Earn(string boosterName, int amount, string source)
        {
            AnalyticsManager.Instance.Track(new BoosterEarnEvent
            {
                BoosterName = boosterName,
                Value = amount,
                Source = source
            });
        }
    }
}
