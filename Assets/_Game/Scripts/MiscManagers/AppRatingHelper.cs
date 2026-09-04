using Titipi.MocaLib.Runtime.Services;
using UnityEngine;
using UserDataPack;

public class AppRatingHelper
{
    public static int LevelToShowRating = 7;

    public static bool HasShownRating
    {
        get => PlayerPrefs.GetInt("AppRatingHelper_HasShownAppRating", 0) == 1;
        set
        {
            PlayerPrefs.SetInt("AppRatingHelper_HasShownAppRating", value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
    
    public static bool TryShowRating()
    {
        if (HasShownRating)
            return false;

        if (AdsManager.SecondsPassSinceLastInterAd < 60f)
            return false;
     
        var userData = UserData.Instance;
        if (userData?.GameplayData == null || userData.GameplayData.CurrentGameplayLevel <= LevelToShowRating)
            return false;
        
        MocaLib.Instance?.RatingManager?.Show();
        HasShownRating = true;
        return true;
    }
}
