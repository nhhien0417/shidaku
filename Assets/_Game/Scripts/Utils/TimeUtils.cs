using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Globalization;

public static class TimeUtils
{
    public static string ToTicksAsString(this DateTime time)
    {
        return time.Ticks.ToString(CultureInfo.CreateSpecificCulture("en-US"));
    }

    public static int GetDaysUntilNextMonday(this DateTime now)
    {
        return now.DayOfWeek == DayOfWeek.Monday ? 7 : ((int) DayOfWeek.Monday - (int) now.DayOfWeek + 7) % 7;
    }

    public static string ToReadableString(this TimeSpan timeSpan, string format = "[d] [hh] [mm] [ss]")
    {
        if (timeSpan.Days <= 0)
        {
            format = format.Replace("[d]", "");

            if (timeSpan.Hours <= 0)
            {
                format = format.Replace("[hh]", "");

                if (timeSpan.Minutes <= 0 && format.IndexOf("[ss]") >= 0)
                {
                    format = format.Replace("[mm]", "");
                }
            }
        }

        format = format.Trim();

        format = format.Replace("[d]", timeSpan.Days.ToString() + "d");
        format = format.Replace("[hh]", timeSpan.Hours.ToString() + "h");
        format = format.Replace("[mm]", timeSpan.Minutes.ToString() + "m");
        format = format.Replace("[ss]", timeSpan.Seconds.ToString());

        return format;
    }

    public static DateTime ToDateTime(this string ticksAtString)
    {
        if (long.TryParse(ticksAtString, out long ticks))
        {
            return new DateTime(ticks);
        }

        if (ticksAtString != "")
            Debug.LogError("Invalid date time format");

        return default(DateTime);
    }

    public static TimeSpan GetTimeLeft(DateTime startTime, DateTime currentTime, double expiredDays)
    {
        DateTime expiredDate = startTime.AddDays(expiredDays);
        return expiredDate - currentTime;
    }
}