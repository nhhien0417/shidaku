namespace _Game.UI.NotifyBadge
{
    public interface IHasNotifyBadgeData
    {
        public string GetNotifyBadgeKey();
        public bool ShouldShowBadge();
        public void DismissBadge();
    }
}