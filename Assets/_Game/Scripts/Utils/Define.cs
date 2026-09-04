using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class DataKey
{
    public const string MAX_UNLOCKED_LEVEL = "MaxUnlockedLevel";
    public const string PLAYING_LEVEL = "PlayingLevel";
    public const string MUSIC_VOLUME = "MusicVolume";
    public const string SFX_VOLUME = "SfxVolume";
    public const string VIBRATION = "Vibration";
    public const string BORDERS = "Borders";
    public const string AUTO_X = "AutoX";
    public const string SHOW_DEBUG_UI = "ShowDebugUI";
}

public class Key
{
    public const string MAIN_MENU_SCENE = "_Game/Scenes/Home";
    public const string GAMEPLAY_SCENE = "_Game/Scenes/Gameplay";
    public const string TUTORIAL_SCENE = "_Game/Scenes/Tutorial";
    public const string LEVEL_PATH = "_Game/Scenes/Levels/";
}

public class Validator
{
    public static bool ExistsLevel(int level)
    {
        string levelPath = Key.LEVEL_PATH + level;
        return Application.CanStreamedLevelBeLoaded(levelPath);
    }

    public static int GetRandomExistsLevel()
    {
        return Random.Range(1, SceneManager.sceneCountInBuildSettings);
    }
}

public class DataHelper
{
    private static TriggerEvents events = new();

    public static void RegisterDataChangedCallback(string s, Action<object> callback)
    {
        events.RegisterEvent(s, callback);
    }

    public static int MaxUnlockedLevel
    {
        get => PlayerPrefs.GetInt(DataKey.MAX_UNLOCKED_LEVEL, 1);
        set => PlayerPrefs.SetInt(DataKey.MAX_UNLOCKED_LEVEL, value);
    }

    public static int PlayingLevel
    {
        get => PlayerPrefs.GetInt(DataKey.PLAYING_LEVEL, 1);
        set => PlayerPrefs.SetInt(DataKey.PLAYING_LEVEL, value);
    }

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(DataKey.MUSIC_VOLUME, 1);
        set
        {
            PlayerPrefs.SetFloat(DataKey.MUSIC_VOLUME, value);
            events.TriggerEvent(value, DataKey.MUSIC_VOLUME);
        }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(DataKey.SFX_VOLUME, 1);
        set
        {
            PlayerPrefs.SetFloat(DataKey.SFX_VOLUME, value);
            events.TriggerEvent(value, DataKey.SFX_VOLUME);
        }
    }

    public static bool Vibration
    {
        get => PlayerPrefs.GetInt(DataKey.VIBRATION, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(DataKey.VIBRATION, value ? 1 : 0);
            events.TriggerEvent(value, DataKey.VIBRATION);
        }
    }

    public static bool Borders
    {
        get
        {
            if (!PlayerPrefs.HasKey(DataKey.BORDERS))
                return DefaultEnableBorders;

            return PlayerPrefs.GetInt(DataKey.BORDERS, 0) == 1;
        }
        set
        {
            PlayerPrefs.SetInt(DataKey.BORDERS, value ? 1 : 0);
            events.TriggerEvent(value, DataKey.BORDERS);
        }
    }

    public static bool DefaultEnableBorders = false;

    public static bool AutoX
    {
        get => PlayerPrefs.GetInt(DataKey.AUTO_X, 1) == 1 && UserDataPack.UserData.Instance.AutoXActivated();
        set
        {
            PlayerPrefs.SetInt(DataKey.AUTO_X, value ? 1 : 0);
            events.TriggerEvent(value, DataKey.AUTO_X);
        }
    }

    public static int TimeOffsetInSeconds
    {
        get => PlayerPrefs.GetInt(DateTimeManager.PlayerPrefsKey.DATETIME_OFFSET_IN_SECONDS, 0);
        set => PlayerPrefs.SetInt(DateTimeManager.PlayerPrefsKey.DATETIME_OFFSET_IN_SECONDS, value);
    }

    public static bool UseDeviceTimeAsServerTime
    {
        get => PlayerPrefs.GetInt(DateTimeManager.PlayerPrefsKey.USE_DEVICE_TIME_AS_SERVER_TIME, 0) == 1;
        set => PlayerPrefs.SetInt(DateTimeManager.PlayerPrefsKey.USE_DEVICE_TIME_AS_SERVER_TIME, value ? 1 : 0);
    }

    public static bool ShowDebugUI
    {
        get => PlayerPrefs.GetInt(DataKey.SHOW_DEBUG_UI, 0) == 1;
        set => PlayerPrefs.SetInt(DataKey.SHOW_DEBUG_UI, value ? 1 : 0);
    }
}

public class TriggerEvents
{
    private Dictionary<string, Action<object>> eventsTrigger = new();

    public void RegisterEvent(string s, Action<object> callback)
    {
        eventsTrigger[s] = callback;
    }

    public void AppendEvent(string s, Action<object> callback)
    {
        if (eventsTrigger.ContainsKey(s))
        {
            eventsTrigger[s] += callback;
        }
        else
        {
            RegisterEvent(s, callback);
        }
    }

    public void TriggerEvent(object data, string key)
    {
        if (eventsTrigger.ContainsKey(key))
            eventsTrigger[key].Invoke(data);
    }
}