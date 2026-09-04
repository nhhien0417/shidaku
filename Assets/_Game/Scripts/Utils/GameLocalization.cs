using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class GameLocalization
{
    private const string FallbackLocaleCode = "en";
    private static readonly List<Locale> Locales = new();
    private static int _currentIndex = -1;

    public static IEnumerator InitializeRoutine()
    {
        var initOperation = LocalizationSettings.InitializationOperation;
        var timeout = 5f;

        while (!initOperation.IsDone && timeout > 0)
        {
            yield return null;
            timeout -= Time.deltaTime;
        }

        if (timeout <= 0f)
        {
            Debug.LogError("[GameLocalization] Localization initialization timed out");
            yield break;
        }

        CacheLocales();

        if (LocalizationSettings.SelectedLocale == null)
        {
            Debug.Log($"[GameLocalization] Selected locale not found. Using fallback locale: {FallbackLocaleCode}");
            SetLocale(GetLocale(FallbackLocaleCode));
        }
        else
            _currentIndex = Locales.IndexOf(LocalizationSettings.SelectedLocale);
    }

    public static int AvailableLocaleCount
    {
        get
        {
            EnsureLocaleCache();
            return Locales.Count;
        }
    }

    public static Locale CurrentLocale
    {
        get
        {
            var locale = LocalizationSettings.SelectedLocale;
            return locale != null ? locale : GetLocale(FallbackLocaleCode);
        }
    }

    public static bool SelectNextLocale()
        => SelectLocaleOffset(1);

    public static bool SelectPreviousLocale()
        => SelectLocaleOffset(-1);

    public static string GetCurrentLocaleDisplayName()
        => GetLocaleDisplayName(CurrentLocale);

    public static string GetLocaleDisplayName(Locale locale)
    {
        if (locale == null)
            return "English";

        var code = locale.Identifier.Code;
        if (!string.IsNullOrEmpty(code))
        {
            try
            {
                var culture = CultureInfo.GetCultureInfo(code);
                var languageCulture = culture.IsNeutralCulture || culture.Parent == CultureInfo.InvariantCulture
                    ? culture
                    : culture.Parent;

                return CapitalizeFirst(languageCulture.NativeName);
            }
            catch (CultureNotFoundException)
            {
                if (!string.IsNullOrEmpty(locale.LocaleName))
                    return locale.LocaleName;
            }
        }

        return string.IsNullOrEmpty(locale.LocaleName) ? "English" : locale.LocaleName;
    }

    private static string CapitalizeFirst(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text.Length == 1
            ? text.ToUpper()
            : char.ToUpper(text[0]) + text[1..];
    }

    private static bool SelectLocaleOffset(int offset)
    {
        EnsureLocaleCache();

        var count = Locales.Count;
        if (count <= 1)
            return false;

        if (_currentIndex < 0 || LocalizationSettings.SelectedLocale != Locales[_currentIndex])
            _currentIndex = Locales.IndexOf(LocalizationSettings.SelectedLocale);

        var nextIndex = _currentIndex >= 0
            ? (_currentIndex + offset + count) % count
            : offset >= 0 ? 0 : count - 1;

        SetLocale(Locales[nextIndex]);
        return true;
    }

    private static void SetLocale(Locale locale)
    {
        if (locale == null)
            return;

        LocalizationSettings.SelectedLocale = locale;
        _currentIndex = Locales.IndexOf(locale);
    }

    private static Locale GetLocale(string code)
    {
        if (string.IsNullOrEmpty(code))
            return null;

        EnsureLocaleCache();
        for (int i = 0; i < Locales.Count; i++)
        {
            var locale = Locales[i];
            if (locale != null && locale.Identifier.Code == code)
                return locale;
        }

        return null;
    }

    private static void EnsureLocaleCache()
    {
        if (Locales.Count == 0)
            CacheLocales();
    }

    private static void CacheLocales()
    {
        Locales.Clear();

        var availableLocales = LocalizationSettings.AvailableLocales?.Locales;
        if (availableLocales == null)
            return;

        foreach (var locale in availableLocales)
        {
            if (locale != null)
                Locales.Add(locale);
        }

        _currentIndex = Locales.IndexOf(LocalizationSettings.SelectedLocale);
    }
}
