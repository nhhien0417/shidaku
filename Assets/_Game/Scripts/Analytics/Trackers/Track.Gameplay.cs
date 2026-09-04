using System;
using Analytics;
using Analytics.Events;
using Design.Ids;
using Gameplay;
using UnityEngine;
using UserDataPack;
using UserDataPack.PropertyDataGroup;
using UserDataPack.Structures.GameplayDataGroup;

public static partial class Track
{
    public static class Gameplay
    {
        private static int _level;
        private static string _playMode;
        private static string _baseMode;
        private static string _puzzleId;
        private static string _hardLabel;
        private static int _heart;
        private static int _heartRemain;

        private static int _attemptNum;
        private static float _attemptStartTime;

        private static float _continuedDuration;
        private static float _lastFailDuration;
        private static string _lastFailReason;
        private static int _lastFailMove;

        private static int _move;
        private static int _boosterQueen;
        private static int _boosterHint;
        private static int _boosterRandomMark;
        private static int _coinSpend;
        private static int _interAd;
        private static int _rvAd;
        private static float _adDuration;
        private static float _percentCompleted;
        private static double _adRevenue;

        private static bool _hasWon;
        private static bool _isActive;
        private static bool _hasPendingFailChoice;

        private static bool _sessionDdaEnabled;
        private static string _sessionDifficulty;
        private static string _sessionBaseDifficulty;
        private static string _sessionUserDifficulty;

        public static void Begin(string playMode, string baseMode, int levelNum, string puzzleId, int heart, string hardLabel)
        {
            _playMode = playMode;
            _baseMode = baseMode ?? BaseMode.None;
            _level = levelNum;
            _puzzleId = puzzleId;
            _heart = heart;
            _heartRemain = heart;
            _hardLabel = hardLabel;

            _hasWon = false;
            _isActive = true;
            _hasPendingFailChoice = false;
            _percentCompleted = 0f;

            var tracking = GetOrCreateTracking(puzzleId);
            tracking.PlayMode = playMode;
            _attemptNum = tracking.AttemptNum;
            _attemptStartTime = Time.realtimeSinceStartup;
            _continuedDuration = 0f;

            ResetAttempt();
            SyncContext();

            if (playMode == "normal" && string.IsNullOrEmpty(baseMode))
            {
                var levelConfigs = Design.DesignDataHolder.Instance != null ? Design.DesignDataHolder.Instance.LevelConfigs : null;
                _sessionDdaEnabled = levelConfigs != null && levelConfigs.LevelDdaEnabled;

                float offset = _sessionDdaEnabled && UserData.Instance != null && UserData.Instance.GameplayData != null
                    ? UserData.Instance.GameplayData.UserDifficulty
                    : 0f;
                float baseScore = levelConfigs != null ? levelConfigs.GetBaseLevelDifficulty(levelNum) : 0f;
                float finalScore = baseScore > 0f ? baseScore + offset : baseScore;

                int baseIndex = 1;
                int finalIndex = 1;
                var poolEntries = levelConfigs?.PuzzlePool?.PoolEntries;
                if (poolEntries != null)
                {
                    for (int i = 0; i < poolEntries.Count; i++)
                    {
                        var r = poolEntries[i].Range;
                        if (r != null && r.Count == 2)
                        {
                            if (baseScore >= r[0] && baseScore < r[1]) baseIndex = i + 1;
                            if (finalScore >= r[0] && finalScore < r[1]) finalIndex = i + 1;
                        }
                    }
                }

                _sessionDifficulty = finalIndex.ToString();
                _sessionBaseDifficulty = baseIndex.ToString();
                _sessionUserDifficulty = (finalIndex - baseIndex).ToString();

                string finalPoolKey = levelConfigs != null ? levelConfigs.GetFinalPoolKey(levelNum, baseScore > 0 ? offset : 0) : "unknown";
                var tagAndRange = levelConfigs != null ? levelConfigs.GetLevelTagAndRange(levelNum, baseScore > 0 ? offset : 0) : ("unknown", 0f, 0f);

                Debug.Log($"[DDA Debug] ===== START NORMAL LEVEL {levelNum} =====\n" +
                          $" - Puzzle ID (Asset Path): {puzzleId}\n" +
                          $" - Base Difficulty Score: {baseScore}\n" +
                          $" - Current Difficult Offset: {(baseScore > 0 ? offset : 0)}\n" +
                          $" - Final Difficulty Score: {finalScore}\n" +
                          $" - Target Pool Key (Tag): {finalPoolKey}\n" +
                          $" - Addressable Label: {tagAndRange.Item1} (Range: [{tagAndRange.Item2}, {tagAndRange.Item3}])");
            }
            else
            {
                _sessionDifficulty = "";
                _sessionBaseDifficulty = "";
                _sessionUserDifficulty = "";
                _sessionDdaEnabled = false;
            }

            LevelStart();
        }

        public static void Retry()
        {
            _isActive = true;
            _hasPendingFailChoice = false;
            _percentCompleted = 0f;

            var tracking = GetOrCreateTracking(_puzzleId);
            _attemptNum = tracking.AttemptNum;
            _attemptStartTime = Time.realtimeSinceStartup;
            _continuedDuration = 0f;

            ResetAttempt(true);
            SyncContext();
            LevelStart();
        }

        public static void End(bool success, string reason, float percentCompleted)
        {
            if (!_isActive) return;
            _isActive = false;
            _percentCompleted = Mathf.Clamp(percentCompleted, 0f, 100f);

            var segmentDuration = GetCurrentAttemptSegmentDuration();
            var duration = _continuedDuration + segmentDuration;

            AnalyticsManager.Instance.Track(new LevelEndEvent
            {
                Level = _level,
                PlayMode = _playMode,
                BaseMode = _baseMode,
                PuzzleId = _puzzleId,
                Success = success,
                FailReason = reason,
                PercentCompleted = _percentCompleted,
                Duration = duration,
                HeartRemain = success ? _heartRemain : 0,
                HardLabel = _hardLabel,
                AdDuration = _adDuration,
                AdRevenue = _adRevenue,
                AttemptNum = _attemptNum,
                BoosterQueenUsed = _boosterQueen,
                BoosterHintUsed = _boosterHint,
                BoosterRandomMarkUsed = _boosterRandomMark,
                CoinSpend = _coinSpend,
                InterAdWatched = _interAd,
                RvAdWatched = _rvAd,
                Difficulty = _sessionDifficulty,
                BaseDifficulty = _sessionBaseDifficulty,
                UserDifficulty = _sessionUserDifficulty,
                DdaEnabled = _sessionDdaEnabled
            });

            var tracking = GetOrCreateTracking(_puzzleId);
            tracking.Duration += segmentDuration;

            if (!success)
            {
                tracking.AttemptNum++;
                _lastFailDuration = duration;
                _lastFailReason = reason;
                _lastFailMove = _move;
                _hasPendingFailChoice = true;
            }
            else
            {
                _hasPendingFailChoice = false;
            }

            ResetAttempt();

            var userData = UserData.Instance;

            if (success && !_hasWon)
            {
                _hasWon = true;

                var levelConfigs = Design.DesignDataHolder.Instance?.LevelConfigs;
                int levelStartAdjustment = levelConfigs?.AdjustRule?.LevelStartAdjustment ?? -1;
                if (levelConfigs != null
                    && levelConfigs.LevelDdaEnabled
                    && GameplayManager.CurrentMode == GameMode.Normal
                    && _playMode == "normal"
                    && string.IsNullOrEmpty(_baseMode)
                    && levelStartAdjustment <= userData.GameplayData.CurrentGameplayLevel)
                {
                    float minAdjustment = levelConfigs.AdjustRule.MinAdjustment;
                    float maxAdjustment = levelConfigs.AdjustRule.MaxAdjustment;
                    float adjustmentValue = levelConfigs.AdjustRule.AdjustmentValue;
                    float currentOffset = userData.GameplayData.UserDifficulty;

                    bool shouldIncrease = false;
                    foreach (var cond in levelConfigs.AdjustRule.Increase)
                    {
                        if (Design.Conditions.ConditionHelper.ConditionsIsValidAndMet(cond))
                        {
                            shouldIncrease = true;
                            break;
                        }
                    }

                    bool shouldDecrease = false;
                    foreach (var cond in levelConfigs.AdjustRule.Decrease)
                    {
                        if (Design.Conditions.ConditionHelper.ConditionsIsValidAndMet(cond))
                        {
                            shouldDecrease = true;
                            break;
                        }
                    }

                    if (shouldIncrease)
                    {
                        userData.GameplayData.UserDifficulty = Math.Clamp(currentOffset + adjustmentValue, minAdjustment, maxAdjustment);
                        Debug.Log($"[DDA] Offset adjusted from {currentOffset} to {userData.GameplayData.UserDifficulty}");
                    }
                    else if (shouldDecrease)
                    {
                        userData.GameplayData.UserDifficulty = Math.Clamp(currentOffset - adjustmentValue, minAdjustment, maxAdjustment);
                        Debug.Log($"[DDA] Offset adjusted from {currentOffset} to {userData.GameplayData.UserDifficulty}");
                    }
                }

                AnalyticsManager.Instance.Track(new CheckpointLevelEvent
                {
                    Level = _level,
                    PlayMode = _playMode,
                    BaseMode = _baseMode,
                    PuzzleId = _puzzleId,
                    HeartRemain = _heartRemain,
                    HardLabel = _hardLabel,
                    AttemptNum = _attemptNum,
                    Duration = tracking.Duration,
                    BoosterQueenUsed = tracking.BoosterQueen,
                    BoosterHintUsed = tracking.BoosterHint,
                    BoosterRandomMarkUsed = tracking.BoosterRandomMark,
                    CoinSpend = tracking.CoinSpend,
                    InterAdWatched = tracking.InterAd,
                    RvAdWatched = tracking.RvAd,
                    Difficulty = _sessionDifficulty,
                    BaseDifficulty = _sessionBaseDifficulty,
                    UserDifficulty = _sessionUserDifficulty,
                    DdaEnabled = _sessionDdaEnabled
                });

                AppsflyerTrackingHelper.TrackFirstTimeCompleteLevel(_level, _playMode);

                tracking.IsWon = true;
                PruneTracking();
            }

            userData.Save();
        }

        public static void Fail(string reason, float percentCompleted)
        {
            if (!_isActive && !_hasPendingFailChoice) return;

            var duration = _isActive ? GetCurrentPlayingTime() : _lastFailDuration;
            var move = _isActive ? _move : _lastFailMove;

            if (_isActive)
            {
                _percentCompleted = Mathf.Clamp(percentCompleted, 0f, 100f);
            }

            AnalyticsManager.Instance.Track(new LevelFailEvent
            {
                Level = _level,
                PlayMode = _playMode,
                BaseMode = _baseMode,
                Move = move,
                Duration = duration,
                HardLabel = _hardLabel,
                FailReason = string.IsNullOrEmpty(reason) ? _lastFailReason : reason,
                InternetConnection = GetInternetConnection(),
                Difficulty = _sessionDifficulty,
                BaseDifficulty = _sessionBaseDifficulty,
                UserDifficulty = _sessionUserDifficulty,
                DdaEnabled = _sessionDdaEnabled
            });

            _hasPendingFailChoice = false;
        }

        public static void Continue(string failReason)
        {
            if (!_hasPendingFailChoice) return;

            AnalyticsManager.Instance.Track(new LevelContinueEvent
            {
                Level = _level,
                PlayMode = _playMode,
                BaseMode = _baseMode,
                Duration = _lastFailDuration,
                HardLabel = _hardLabel,
                FailReason = string.IsNullOrEmpty(failReason) ? _lastFailReason : failReason,
                Difficulty = _sessionDifficulty,
                BaseDifficulty = _sessionBaseDifficulty,
                UserDifficulty = _sessionUserDifficulty,
                DdaEnabled = _sessionDdaEnabled
            });

            _isActive = true;
            _hasPendingFailChoice = false;
            _attemptStartTime = Time.realtimeSinceStartup;
            _continuedDuration = _lastFailDuration;
            _percentCompleted = 0f;
            ResetAttempt(true);
        }

        public static void OnMove()
        {
            if (!_isActive) return;
            _move++;
        }

        public static void SetHeartRemain(int heartRemain)
        {
            _heartRemain = Mathf.Max(0, heartRemain);
        }

        public static void OnBoosterUsed(string boosterName)
        {
            if (!_isActive) return;

            var tracking = GetOrCreateTracking(_puzzleId);

            if (boosterName == BoosterName.Queen)
            {
                _boosterQueen++;
                tracking.BoosterQueen++;
            }
            else if (boosterName == BoosterName.Hint)
            {
                _boosterHint++;
                tracking.BoosterHint++;
            }
            else if (boosterName == BoosterName.RandomMark)
            {
                _boosterRandomMark++;
                tracking.BoosterRandomMark++;
            }

            UserData.Instance.Save();
            Booster.Spend(_level, _playMode, _puzzleId, boosterName);
        }

        public static void OnInterAd(float durationSeconds = 0f)
        {
            if (!_isActive && !_hasPendingFailChoice) return;

            var tracking = GetOrCreateTracking(_puzzleId);
            tracking.InterAd++;

            _interAd++;
            _adDuration += durationSeconds;

            UserData.Instance.Save();
        }

        public static void OnRvAd(float durationSeconds = 0f)
        {
            if (!_isActive && !_hasPendingFailChoice) return;

            var tracking = GetOrCreateTracking(_puzzleId);
            tracking.RvAd++;

            _rvAd++;
            _adDuration += durationSeconds;

            UserData.Instance.Save();
        }

        public static void OnAdRevenue(double value)
        {
            if (!_isActive && !_hasPendingFailChoice) return;

            _adRevenue += System.Math.Max(0d, value);
        }

        public static void OnCoinSpent(int amount)
        {
            if (!_isActive) return;

            _coinSpend += amount;

            var tracking = GetOrCreateTracking(_puzzleId);
            tracking.CoinSpend += amount;
            UserData.Instance.Save();
        }

        public static void UpdatePercentCompleted(float percentCompleted)
        {
            if (!_isActive) return;
            _percentCompleted = Mathf.Clamp(percentCompleted, 0f, 100f);
        }

        private static void LevelStart()
        {
            AnalyticsManager.Instance.Track(new LevelStartEvent
            {
                Level = _level,
                PlayMode = _playMode,
                BaseMode = _baseMode,
                PuzzleId = _puzzleId,
                Heart = _heart,
                HardLabel = _hardLabel,
                AttemptNum = _attemptNum,
                RemainingCoins = UserData.Instance.GetResourceItemAmount(ItemId.Coin),
                InternetConnection = GetInternetConnection(),
                Difficulty = _sessionDifficulty,
                BaseDifficulty = _sessionBaseDifficulty,
                UserDifficulty = _sessionUserDifficulty,
                DdaEnabled = _sessionDdaEnabled
            });
        }

        private static void SyncContext()
        {
            AnalyticsContext.CurrentPlayMode = _playMode;
            AnalyticsContext.CurrentLevel = _level;
            AnalyticsContext.CurrentPuzzleId = _puzzleId;
        }

        private static void ResetAttempt(bool keepAdStats = false)
        {
            if (!keepAdStats)
            {
                _adDuration = 0f;
                _adRevenue = 0d;
                _interAd = 0;
                _rvAd = 0;
            }

            _move = 0;
            _boosterQueen = 0;
            _boosterHint = 0;
            _boosterRandomMark = 0;
            _coinSpend = 0;
        }

        private static string GetInternetConnection()
        {
            return Application.internetReachability == NetworkReachability.NotReachable ? "offline" : "online";
        }

        private static float GetCurrentPlayingTime()
        {
            return _continuedDuration + GetCurrentAttemptSegmentDuration();
        }

        private static float GetCurrentAttemptSegmentDuration()
        {
            return Mathf.Max(0f, Time.realtimeSinceStartup - _attemptStartTime);
        }

        private static LevelTrackingData GetOrCreateTracking(string puzzleId)
        {
            var list = UserData.Instance.GameplayData.LevelTracking;
            var entry = list.Find(t => t.PuzzleId == puzzleId);

            if (entry == null)
            {
                entry = new LevelTrackingData { PuzzleId = puzzleId, AttemptNum = 1 };
                list.Add(entry);
            }

            return entry;
        }

        private static void PruneTracking()
        {
            var list = UserData.Instance.GameplayData.LevelTracking;
            if (list == null) return;

            var window = 10;
            if (Design.DesignDataHolder.Instance != null && Design.DesignDataHolder.Instance.LevelConfigs != null)
            {
                window = Design.DesignDataHolder.Instance.LevelConfigs.AdjustRule.Window;
            }

            var wonEntries = list.FindAll(t => t.IsWon && t.PlayMode == "normal");
            if (wonEntries.Count > window)
            {
                int removeCount = wonEntries.Count - window;
                for (int i = 0; i < removeCount; i++)
                {
                    list.Remove(wonEntries[i]);
                }
            }
        }
    }
}
