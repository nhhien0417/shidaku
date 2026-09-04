using System.Collections.Generic;
using System.ComponentModel;
using _Game.Scripts.Cheat;
using _Game.UI.NotifyBadge;
using Design;
using Design.Ids;
using Game.PromotionOffer;
using Gameplay;
using HapticFeedback;
using LevelDesign;
using UnityEngine;
using UserDataPack;
using Game.InappMessageHandlers;
using RemoteConfigs;
using UI.Common;

public partial class SROptions
{
    [Category("Tutorial")]
    public void SkipTutorial()
    {
        var userData = UserData.Instance;
        userData.GameplayData.TutorialCompleted = true;
        userData.GameplayData.CurrentLevelIndex = 5;
        userData.Save();
        NavigationController.Instance.LoadToMainMenu();
    }

    private int _level = 0;
    private string _puzzleId = string.Empty;

    [Category("Level"), Sort(0)]
    public int Level
    {
        get => _level;
        set => _level = value;
    }

    [Category("Level"), Sort(1)]
    public void LoadLevel()
    {
        var userData = UserData.Instance;
        userData.GameplayData.TutorialCompleted = true;
        userData.GameplayData.CurrentLevelIndex = _level - 1;
        userData.Save();
        NavigationController.Instance.LoadToGameplay();
        UIManager.Instance.HideAllUI();
    }

    [Category("Level"), Sort(2)]
    public void CompleteLevel()
    {
        var boardManager = Object.FindAnyObjectByType<BoardManager>();
        if (boardManager != null)
        {
            boardManager.FillAllBoard();
            boardManager.OnBoardChanged?.Invoke();
        }
    }

    [Category("Level"), Sort(3)]
    public void FailLevel()
    {
        var gameplayManager = Object.FindAnyObjectByType<GameplayManager>();
        if (gameplayManager != null)
        {
            gameplayManager.LoseLife();
            gameplayManager.LoseLife();
            gameplayManager.LoseLife();
        }
    }

    [Category("Level"), Sort(4)]
    public string PuzzleId
    {
        get => _puzzleId;
        set => _puzzleId = value;
    }

    [Category("Level"), Sort(5)]
    public void LoadPuzzle()
    {
        var puzzleId = PuzzleId?.Trim();
        if (string.IsNullOrEmpty(puzzleId))
        {
            return;
        }

        var userData = UserData.Instance;
        userData.GameplayData.TutorialCompleted = true;
        userData.Save();
        LevelManager.RequestCheatPuzzle(puzzleId);
        NavigationController.Instance.LoadToGameplay();
        UIManager.Instance.HideAllUI();
    }

    [Category("DDA"), Sort(0)]
    public float DDAOffset
    {
        get => UserData.Instance != null && UserData.Instance.GameplayData != null ? UserData.Instance.GameplayData.UserDifficulty : 0f;
        set
        {
            if (UserData.Instance != null && UserData.Instance.GameplayData != null)
            {
                UserData.Instance.GameplayData.UserDifficulty = value;
                UserData.Instance.Save();
            }
        }
    }

    private int _leaderboardPoint = 0;

    [Category("Leaderboard"), Sort(0)]
    public int LeaderboardPoint
    {
        get => _leaderboardPoint;
        set => _leaderboardPoint = Mathf.Max(0, value);
    }

    [Category("Leaderboard"), Sort(1)]
    public void UpdateLeaderboardPoint()
    {
        var userData = UserData.Instance;
        userData.UserProfile.LeaderboardPoint = _leaderboardPoint;
        userData.UserProfile.HasSubmitLeaderboardPoint = false;
        userData.Save();

        userData.UserProfile.SubmitLeaderboardScore();
    }

    private int _completeArtThroughLevel = 1;

    [Category("Art"), Sort(0)]
    public int CompleteArtThroughLevel
    {
        get => _completeArtThroughLevel;
        set => _completeArtThroughLevel = Mathf.Max(0, value);
    }

    [Category("Art"), Sort(1)]
    public void CompleteArtThroughUnlockLevel()
    {
        var userData = UserData.Instance;
        userData.ArtPuzzleData.CompleteThroughUnlockLevel(_completeArtThroughLevel);
        userData.Save();
    }

    [Category("Art"), Sort(2)]
    public void ResetArtProgress()
    {
        var userData = UserData.Instance;
        userData.ArtPuzzleData.ResetProgress();

        userData.GameplayData.PlayedIdsPerTag.RemoveAll(entry => entry.Tag == LevelType.Art.ToTag());

        if (GameplayManager.CurrentMode == GameMode.Art)
        {
            GameplayManager.CurrentMode = GameMode.Normal;
        }

        userData.Save();
        userData.ArtPuzzleData.RefreshUnlockArtPuzzleNotification(userData.GameplayData.CurrentGameplayLevel);
    }

    public enum CheatItemType
    {
        Queen = 1,
        Hint = 2,
        RandomMark = 3,
        AutoXLimitedTime = 4,
        NoAdsLimitedTime = 5,
    }
    private CheatItemType _cheatItemType = CheatItemType.Queen;
    private int _itemAmount = 5;

    [Category("Items"), Sort(0)]
    public CheatItemType SelectedItem
    {
        get => _cheatItemType;
        set => _cheatItemType = value;
    }

    [Category("Items"), Sort(1)]
    public int BoosterAmount
    {
        get => _itemAmount;
        set => _itemAmount = value;
    }

    [Category("Items"), Sort(2)]
    public void AddItem()
    {
        var itemId = _cheatItemType switch
        {
            CheatItemType.Queen => ItemId.Booster_1,
            CheatItemType.Hint => ItemId.Booster_2,
            CheatItemType.RandomMark => ItemId.Booster_3,
            CheatItemType.AutoXLimitedTime => ItemId.AutoXLimitedTime,
            CheatItemType.NoAdsLimitedTime => ItemId.NoAdsLimitedTime,
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(itemId)) return;

        UserData.Instance.AddItem(itemId, _itemAmount, "cheat");
        UserData.Instance.Save();
    }

    [Category("Daily Challenge"), Sort(0)]
    public void WinSelectedDay()
    {
        UserData.Instance.DailyChallengeData.RecordWin(UIDailyChallenge.SelectedDay);
    }

    [Category("Daily Challenge"), Sort(1)]
    public void Win5Days()
    {
        var daily = UserData.Instance.DailyChallengeData;
        var selectedDate = UIDailyChallenge.SelectedDay;
        var daysInMonth = System.DateTime.DaysInMonth(selectedDate.Year, selectedDate.Month);
        var added = 0;
        for (var i = 1; i <= daysInMonth && added < 5; i++)
        {
            var date = new System.DateTime(selectedDate.Year, selectedDate.Month, i);
            if (date > selectedDate) break;
            if (!daily.HasWon(date))
            {
                daily.RecordWin(date);
                added++;
            }
        }
    }

    [Category("Daily Challenge"), Sort(2)]
    public void UnlockAllDays()
    {
        var daily = UserData.Instance.DailyChallengeData;
        var selectedDate = UIDailyChallenge.SelectedDay;
        var daysInMonth = System.DateTime.DaysInMonth(selectedDate.Year, selectedDate.Month);
        for (int i = 1; i <= daysInMonth; i++)
        {
            daily.UnlockDay(new System.DateTime(selectedDate.Year, selectedDate.Month, i));
        }
        UserData.Instance.Save();
    }

    [Category("Daily Challenge"), Sort(3)]
    public void ResetAllDays()
    {
        var daily = UserData.Instance.DailyChallengeData;
        var selectedDate = UIDailyChallenge.SelectedDay;
        var monthKey = selectedDate.ToMonthKey();

        // Remove all wins for this month
        var keysToRemove = new List<string>();
        foreach (var win in daily.WonDays)
        {
            if (win.Key.StartsWith(monthKey)) keysToRemove.Add(win.Key);
        }
        foreach (var key in keysToRemove) daily.WonDays.Remove(key);

        // Remove claimed milestones
        if (daily.ClaimedMonthRewards.ContainsKey(monthKey))
        {
            daily.ClaimedMonthRewards.Remove(monthKey);
        }

        BadgeNotificationManager.Instance.RefreshDailyChallengeBadges();
        UserData.Instance.Save();
    }

    private int _streak = 1;

    [Category("Day Streak"), Sort(0)]
    public int Streak
    {
        get => _streak;
        set => _streak = value;
    }

    [Category("Day Streak"), Sort(1)]
    public void SetCurrentStreak()
    {
        UserData.Instance.DayStreakData.LastStreakDateTicks = DateTimeManager.Now.Date.AddDays(-1).Ticks;
        UserData.Instance.DayStreakData.CurrentStreak = _streak;
        UserData.Instance.Save();
    }

    [Category("Day Streak"), Sort(2)]
    public void SetMaxStreak()
    {
        UserData.Instance.DayStreakData.MaxStreak = _streak;
        UserData.Instance.Save();
    }

    [Category("Day Streak"), Sort(3)]
    public void SetLostStreak()
    {
        var missedDay = Mathf.Min(-2, -(_streak + 1));
        UserData.Instance.DayStreakData.LastStreakDateTicks = DateTimeManager.Now.Date.AddDays(missedDay).Ticks;
        UserData.Instance.Save();
    }

    [Category("Shop")]
    public void RefreshDailyShopItems()
    {
        var userData = UserData.Instance;
        userData.TrackingData.LastDailyBoughtTimesDate = "";
        userData.TrackingData.DailyBoughtTimesData.Clear();
        userData.TrackingData.DailyBoughtTimesSlotData.Clear();
        userData.Save();

        var topUI = UIManager.Instance?.TopUIGroup;
        if (topUI != null && topUI.GetType().Name == nameof(UIShop))
        {
            topUI.Show();
        }
    }

    private FeedbackType _vibrationType = FeedbackType.Selection;

    [Category("Vibration"), Sort(0)]
    public FeedbackType SelectedVibration
    {
        get => _vibrationType;
        set => _vibrationType = value;
    }

    [Category("Vibration"), Sort(1)]
    public void TestVibration()
    {
        GameVibration.Instance.Haptic(_vibrationType);
    }

    [Category("Promotion Offers")]
    public void ActiveStarterPack()
    {
        PromotionOfferManager.Instance.HandleReceivedBannerMessage(new()
        {
            CustomData = new Dictionary<string, string>()
            {
                { InAppMessageDataKey.Action, MessageActionKey.ActiveOffer },
                { InAppMessageDataKey.OfferId, PredefinedOfferId.StarterPack },
                { InAppMessageDataKey.Priority, "0" },
                { InAppMessageDataKey.DurationInHours, "1" },
                { InAppMessageDataKey.CustomData, "" },
            }
        });
    }

    [Category("Promotion Offers")]
    public void RemoveStarterPack()
    {
        PromotionOfferManager.Instance.ActivatedOffers.RemoveAll(x => x.OfferData.Id == PredefinedOfferId.StarterPack);
        UserData.Instance.PromotionOfferData.RemoveOffer(PredefinedOfferId.StarterPack);
        UserData.Instance.Save();
    }

    [Category("Promotion Offers")]
    public void ActiveNoAdsDiscount()
    {
        PromotionOfferManager.Instance.HandleReceivedBannerMessage(new()
        {
            CustomData = new Dictionary<string, string>()
            {
                { InAppMessageDataKey.Action, MessageActionKey.ActiveOffer },
                { InAppMessageDataKey.OfferId, PredefinedOfferId.NoAdsDiscount },
                { InAppMessageDataKey.Priority, "1" },
                { InAppMessageDataKey.DurationInHours, "1" },
                { InAppMessageDataKey.CustomData, "" },
            }
        });
    }

    [Category("Promotion Offers")]
    public void RemoveNoAdsDiscount()
    {
        PromotionOfferManager.Instance.ActivatedOffers.RemoveAll(x => x.OfferData.Id == PredefinedOfferId.NoAdsDiscount);
        UserData.Instance.PromotionOfferData.RemoveOffer(PredefinedOfferId.NoAdsDiscount);
        UserData.Instance.Save();
    }

    [Category("Free Gift")]
    public void AddFreeAvatarFrameGift()
    {
        OneCTAPopupHandler.HandleReceivedBannerMessage(new()
        {
            CustomData = new()
            {
                { InAppMessageDataKey.Action, MessageActionKey.FreeGift },
                { InAppMessageDataKey.Items, $"{ItemId.AvatarFrame}:9" },
                { InAppMessageDataKey.PopupImageUrl, "https://firebasestorage.googleapis.com/v0/b/smart-queens-46043.firebasestorage.app/o/starter_pack_popup.png?alt=media&token=f59f9117-e8eb-4a2a-8967-168493134dc6" },
            }
        });
    }

    [Category("Date Time"), Sort(0)]
    public bool UseDeviceTimeAsServerTime
    {
        get => DataHelper.UseDeviceTimeAsServerTime;
        set => DataHelper.UseDeviceTimeAsServerTime = value;
    }

    [Category("Date Time"), Sort(1)]
    public int TimeOffsetInSeconds { get; set; } = DataHelper.TimeOffsetInSeconds;

    [Category("Date Time"), Sort(2)]
    public void SetTimeOffset()
    {
        DataHelper.TimeOffsetInSeconds = TimeOffsetInSeconds;
    }

    [Category("Date Time"), Sort(3)]
    public void SetTimeOffsetToBeforeStreakLoss()
    {
        var streakData = UserData.Instance.DayStreakData;
        var now = DateTimeManager.Now;
        var lastStreakDate = streakData.LastStreakDateTicks > 0 ? new System.DateTime(streakData.LastStreakDateTicks).Date : now.Date;

        var targetTime = lastStreakDate.AddDays(2).AddSeconds(-10);
        var offsetDelta = (int)System.Math.Round((targetTime - now).TotalSeconds);

        TimeOffsetInSeconds = DataHelper.TimeOffsetInSeconds + offsetDelta;
        DataHelper.TimeOffsetInSeconds = TimeOffsetInSeconds;
    }

    [Category("Get id for tester")]
    public void GetIdForTester()
    {
        var id = SystemInfo.deviceUniqueIdentifier;
        Debug.Log("=== Test ID (copied to clipboard)===");
        Debug.Log(id);
        Debug.Log("===================================================");

        UniClipboard.SetText(id);
    }

    [Category("Get id for tester")]
    public async void GetGAID()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            var result = await System.Threading.Tasks.Task.Run(() =>
            {
                AndroidJNI.AttachCurrentThread();

                try
                {
                    using var unityPlayer =
                        new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity =
                        unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    using var advertisingIdClient =
                        new AndroidJavaClass(
                            "com.google.android.gms.ads.identifier.AdvertisingIdClient");
                    using var info =
                        advertisingIdClient.CallStatic<AndroidJavaObject>(
                            "getAdvertisingIdInfo", activity);

                    return (
                        Id: info.Call<string>("getId"),
                        IsLimitAdTrackingEnabled:
                        info.Call<bool>("isLimitAdTrackingEnabled"));
                }
                finally
                {
                    AndroidJNI.DetachCurrentThread();
                }
            });

            var gaid = result.Id;
            if (string.IsNullOrWhiteSpace(gaid) ||
                gaid == "00000000-0000-0000-0000-000000000000")
            {
                Debug.LogWarning("GAID is unavailable or has been deleted.");
                return;
            }

            Debug.Log(
                $"=== GAID (copied, limit tracking: {result.IsLimitAdTrackingEnabled}) ===");
            Debug.Log(gaid);
            Debug.Log("===================================================");

            UniClipboard.SetText(gaid);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
#else
        Debug.LogWarning("GAID is only available on a physical Android device.");
#endif
    }

    [Category("Extended UIs"), Sort(0)]
    public bool ShowDebugUI
    {
        get => DataHelper.ShowDebugUI;
        set
        {
            DataHelper.ShowDebugUI = value;
            UnityEngine.Object.FindAnyObjectByType<DebugUI>(FindObjectsInactive.Include)?.gameObject.SetActive(value);
        }
    }

    [Category("Extended UIs"), Sort(1)]
    public void ResetDebugUIPosition()
    {
        UnityEngine.Object.FindAnyObjectByType<DebugUI>(FindObjectsInactive.Exclude)?.PnlContent?.ResetToOriginalPosition();
    }

    [Category("MaxSdk")]
    public void OpenMediationDebugger()
    {
        if (MaxSdk.IsInitialized())
        {
            MaxSdk.ShowMediationDebugger();
        }
        else
        {
            Debug.LogWarning("AppLovin SDK is not initialized. Please initialize the SDK before opening the mediation debugger.");
        }
    }

    [Category("UI Flows")]
    public void Win_Default()
    {
        DesignDataHolder.Instance.UIFlowsConfig.WinFlow = UIFlowId.WinFlow.Default;
    }

    [Category("UI Flows")]
    public void Win_ShowLeaderboard()
    {
        DesignDataHolder.Instance.UIFlowsConfig.WinFlow = UIFlowId.WinFlow.ShowLeaderboard;
    }

    public enum CollectionItemType
    {
        CustomizeQueen = 1,
        CustomizeX = 2,
    }
    [Category("Collection Items"), Sort(0)]
    public CollectionItemType SelectedCollectionItemType { get; set; }
    [Category("Collection Items"), Sort(1)]
    public int SelectedCollectionId { get; set; }

    [Category("Collection Items"), Sort(2)]
    public void AddCollectionItem()
    {
        var userData = UserData.Instance;
        switch (SelectedCollectionItemType)
        {
            case CollectionItemType.CustomizeQueen:
                userData.CollectionData.CustomizeQueen.AddOwned(SelectedCollectionId);
                break;
            case CollectionItemType.CustomizeX:
                userData.CollectionData.CustomizeX.AddOwned(SelectedCollectionId);
                break;
        }
        userData.Save();
    }

    [Category("Collection Items"), Sort(3)]
    public void RemoveCollectionItem()
    {
        var userData = UserData.Instance;
        switch (SelectedCollectionItemType)
        {
            case CollectionItemType.CustomizeQueen:
                userData.CollectionData.CustomizeQueen.OwnedIds.Remove(SelectedCollectionId);
                break;
            case CollectionItemType.CustomizeX:
                userData.CollectionData.CustomizeX.OwnedIds.Remove(SelectedCollectionId);
                break;
        }
        userData.Save();
    }

    [Category("Remote Configs")]
    public void ReapplyRemoteConfigs()
    {
        RemoteConfigHelper.Instance?.ApplyRemoteConfigs();
    }
}
