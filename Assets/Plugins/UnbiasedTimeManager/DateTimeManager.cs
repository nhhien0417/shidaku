using System;
using System.Collections;
using System.Collections.Generic;
using MEC;
using Titipi.MocaLib.Runtime.Services;
using UnbiasedTimeManager;
using UnityEngine;

public class DateTimeManager : MonoBehaviour
{
    private bool isFirstTimeUpdated = true;

    public static DateTime Now => GetDateTime(); // This time is UTC time
    public static bool IsUpToDate => UnbiasedTimeByHttp.Instance.IsUpToDate;
    public static bool CheatEnabled { get; set; }

    public static Action<DateTime> OnDateTimeFirstReceived;

    public static void RefreshDateTime()
    {
        UnbiasedTimeByHttp.Instance.RefreshDateTime();
    }

    public static void RefreshDateTimeAsync()
    {
        UnbiasedTimeByHttp.Instance.RefreshDateTimeAsync();
    }

    private static DateTime GetDateTime()
    {
        var now = default(DateTime);
        if (!IsUpToDate)
        {
            var lastTicks = PlayerPrefs.GetString("DateTimeManager_LastUpdatedDateTime", "");
            if (!string.IsNullOrEmpty(lastTicks) && long.TryParse(lastTicks, out var ticks))
            {
                now = new DateTime(ticks, DateTimeKind.Utc);
            }
        }
        else
        {
            now = UnbiasedTimeByHttp.Instance.DateTime;
        }

        if (CheatEnabled)
        {
            var offset = PlayerPrefs.GetInt(PlayerPrefsKey.DATETIME_OFFSET_IN_SECONDS, 0);
            var useDeviceTime = PlayerPrefs.GetInt(PlayerPrefsKey.USE_DEVICE_TIME_AS_SERVER_TIME, 0) == 1;
            if (useDeviceTime)
                return DateTime.UtcNow.AddSeconds(offset);

            return now.AddSeconds(offset);
        }
        else
        {
            return now;
        }
    }

    private void OnTimeReceive(bool isSuccess, DateTime time)
    {
        if (isSuccess && isFirstTimeUpdated)
        {
            isFirstTimeUpdated = false;
            OnDateTimeFirstReceived?.Invoke(time);
        }

        if (isSuccess)
        {
            PlayerPrefs.SetString("DateTimeManager_LastUpdatedDateTime", time.Ticks.ToString());
        }
    }

    private void Awake()
    {
        UnbiasedTimeByHttp.Instance.OnTimeReceive += OnTimeReceive;

        if (IsUpToDate)
        {
            OnTimeReceive(true, Now);
        }
    }

    public abstract class PlayerPrefsKey
    {
        public const string USE_DEVICE_TIME_AS_SERVER_TIME = "DateTimeManager_UseDeviceTimeAsServerTime";
        public const string DATETIME_OFFSET_IN_SECONDS = "DateTimeManager_DateTimeOffsetInSeconds";
    }
}
