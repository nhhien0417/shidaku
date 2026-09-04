using System;

namespace UserDataPack.Structures
{
    [Serializable]
    public class NoAdsLimitedTimeData
    {
        public string ExpiredDate;

        public void AddLimitedTime(int totalSeconds)
        {
            var now = DateTimeManager.Now;

            if (string.IsNullOrEmpty(ExpiredDate))
            {
                ExpiredDate = now.AddSeconds(totalSeconds).ToTicksAsString();
            }
            else
            {
                var lastExpiredDate = ExpiredDate.ToDateTime();
                ExpiredDate = now > lastExpiredDate ? now.AddSeconds(totalSeconds).ToTicksAsString() : lastExpiredDate.AddSeconds(totalSeconds).ToTicksAsString();
            }
        }

        public bool IsActivated()
        {
            return DateTimeManager.IsUpToDate && !IsExpired();
        }

        public bool IsExpired()
        {
            if (string.IsNullOrEmpty(ExpiredDate))
                return true;

            var now = DateTimeManager.Now;
            var expiredDate = ExpiredDate.ToDateTime();
            return now > expiredDate;
        }

        public long GetRemainingSeconds()
        {
            if (string.IsNullOrEmpty(ExpiredDate))
                return 0;

            var now = DateTimeManager.Now;
            var expiredDate = ExpiredDate.ToDateTime();
            return (long)(expiredDate - now).TotalSeconds;
        }
    }
}