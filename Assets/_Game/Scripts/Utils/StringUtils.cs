
using System.Globalization;

public static class StringUtils
{
    public static string ToResourceValueString(this int value)
    {
        return value.ToString("N0");
    }

    public static bool TryParseResourceValueInt(this string value, out int result)
    {
        return int.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result);
    }

    public static bool TryParseFloat(this string value, out float result)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }
}