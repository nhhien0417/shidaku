using Analytics;
using Analytics.Events;
using Design.Ids;
using UserDataPack;

public static partial class Track
{
    public static class Screen
    {
        public static void Open(string screenId)
        {
            var userData = UserData.Instance;

            AnalyticsManager.Instance.Track(new OpenScreenEvent
            {
                ScreenId = screenId,
                Level = userData.GameplayData.CurrentGameplayLevel,
                Coin = userData.GetResourceItemAmount(ItemId.Coin),
                BoosterQueen = userData.GetResourceItemAmount(ItemId.Booster_1),
                BoosterHint = userData.GetResourceItemAmount(ItemId.Booster_2),
                BoosterRandomMark = userData.GetResourceItemAmount(ItemId.Booster_3)
            });
        }
    }
}
