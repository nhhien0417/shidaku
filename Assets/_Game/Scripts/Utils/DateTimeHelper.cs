using System;
using System.Collections.Generic;

public static class DateTimeHelper
{
    public static string ToDateKey(this DateTime date)
    {
        return date.ToString("yyyy-MM-dd");
    }

    public static string ToMonthKey(this DateTime date)
    {
        return date.ToString("yyyy-MM");
    }

    public static DateTime GetCurrentWeekDate(int dayIndex)
    {
        var today = DateTimeManager.Now.Date;
        return today.AddDays(dayIndex - ((int)today.DayOfWeek + 6) % 7);
    }

    public static bool ContainsDate(List<long> dateTicks, DateTime date)
    {
        return dateTicks != null && dateTicks.Contains(date.Date.Ticks);
    }

    public static string FormatElapsedMinutesSeconds(float seconds)
    {
        var timeSpan = TimeSpan.FromSeconds(Math.Max(0f, Math.Floor(seconds)));
        var totalMinutes = (int)timeSpan.TotalMinutes;
        return $"{totalMinutes:00}:{timeSpan.Seconds:00}";
    }
}
