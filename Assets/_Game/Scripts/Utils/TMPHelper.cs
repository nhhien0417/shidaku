public static class TMPHelper
{
    public const string FontRegular = "Fredoka-Regular";
    public const string FontCustom = "Baloo-Stroke_ColorRegion";

    public static string WithCustomFont(string text) =>
        $"<font=\"{FontCustom}\">{text}</font>";

    /// <summary>Wraps text with color and outline font tags.</summary>
    public static string WithCustomFont(string text, string hex) =>
        $"<font=\"{FontCustom}\"><color={hex}>{text}</color></font>";

    /// <summary>Wraps text with a TMP size tag.</summary>
    public static string WithSize(this string text, string size) =>
        $"<size={size}>{text}</size>";

    /// <summary>Wraps text with a TMP color tag.</summary>
    public static string WithColor(this string text, string hex) =>
        $"<color={hex}>{text}</color>";
}
