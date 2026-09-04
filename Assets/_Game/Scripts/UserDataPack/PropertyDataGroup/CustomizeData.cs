using System;

using _Game.UI.NotifyBadge;
using Design;
using Design.DataHolder;

namespace UserDataPack.PropertyDataGroup
{
    [Serializable]
    public class CustomizeData : IUserDataPropertyDataGroup
    {
        public int QueenId = 0;
        public int XId = 0;

        public bool HasSeenUnlockNotification;

        public void FixData()
        {

        }

        public void RefreshUnlockCustomizeNotification(int currentLevel)
        {
            var isUnlocked = DesignDataHolder.Instance.FeatureUnlockData.IsUnlocked(FeatureType.Customize, currentLevel);
            if (!isUnlocked || HasSeenUnlockNotification)
            {
                BadgeNotificationManager.Instance.ClearNotification(BadgeNotificationType.NewFeature_Customize);
                return;
            }

            BadgeNotificationManager.Instance.SetNotification(BadgeNotificationType.NewFeature_Customize, 1);
        }

        public void MarkCustomizeUnlockNotificationSeen(int currentLevel)
        {
            var isUnlocked = DesignDataHolder.Instance.FeatureUnlockData.IsUnlocked(FeatureType.Customize, currentLevel);
            if (!isUnlocked || HasSeenUnlockNotification) return;

            HasSeenUnlockNotification = true;
            UserData.Instance.Save();
            BadgeNotificationManager.Instance.ClearNotification(BadgeNotificationType.NewFeature_Customize);
        }
    }
}
