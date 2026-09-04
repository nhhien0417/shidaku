using System;
using System.Collections.Generic;
using _Game.Scripts.Common;
using RemoteConfigs;
using SimpleJSON;
using Titipi.MocaLib.Runtime.Services;
using UnityEngine;
using Random = System.Random;

namespace Game.Leaderboard
{
    public class LeaderboardManager
    {
        public static float CacheExpiredSeconds = 300f;
        public static int LeaderboardFetchLimit = 1000;

        private static List<LeaderboardEntry> _cachedLeaderboardEntries = new ();
        private static float _lastTimeGetTopLeaderboard = -1;

        public static void ApplyRemoteConfig()
        {
            var config = RemoteConfigHelper.Instance?.GetConfig(RemoteConfigKey.LEADERBOARD_CONFIG, "");
            if (!string.IsNullOrEmpty(config))
            {
                try
                {
                    var jsonData = JSON.Parse(config);
                    if (jsonData == null) return;

                    var refreshInterval = jsonData["refresh_interval"]?.AsFloat ?? 0f;
                    if (refreshInterval >= 1f)
                    {
                        CacheExpiredSeconds = refreshInterval;
                    }

                    var fetchLimit = jsonData["fetch_limit"]?.AsInt ?? 0;
                    if (fetchLimit > 0)
                    {
                        LeaderboardFetchLimit = fetchLimit;
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Failed to parse leaderboard config: {e}");
                }
            }
        }

        public static void SubmitGlobalScore(int score, Action onSuccess, Action<Exception> onError)
        {
            SubmitScore(LeaderboardId.Global, score, onSuccess, onError);

            // Update cache
            var playerId = GetPlayerId();
            if (!string.IsNullOrEmpty(playerId) && !LeaderboardCacheExpired())
            {
                var existingEntry = _cachedLeaderboardEntries.Find(entry => entry.UserId == playerId);
                if (existingEntry != null)
                {
                    existingEntry.Score = Mathf.Max(existingEntry.Score, score);
                    _cachedLeaderboardEntries.Sort((a, b) => b.Score.CompareTo(a.Score));
                }
                else if (MocaLib.Instance.PlayerProfileManager?.CurrentProfile != null)
                {
                    var insertIndex = -1;
                    for (var i = _cachedLeaderboardEntries.Count - 1; i >= 0; i--)
                    {
                        var entry = _cachedLeaderboardEntries[i];
                        if (score > entry.Score)
                        {
                            insertIndex = i;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (insertIndex >= 0)
                    {
                        var currentProfile = MocaLib.Instance.PlayerProfileManager.CurrentProfile;
                        var newEntry = new LeaderboardEntry
                        {
                            UserId = playerId,
                            Score = score,
                            DisplayName = currentProfile.DisplayName,
                            Avatar = currentProfile.Avatar,
                            Metadata = new ()
                        };
                        foreach (var item in currentProfile.Metadata)
                        {
                            newEntry.Metadata[item.Key] = item.Value;
                        }
                        _cachedLeaderboardEntries.Insert(insertIndex, newEntry);
                    }
                }
            }
        }

        /// <summary>
        /// Submit score then return 20 entries around user (including user) with rank offset (to determine which rank entry[0] is)
        /// </summary>
        /// <param name="score"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onError"></param>
        public static void SubmitGlobalScore(int score, Action<List<LeaderboardEntry>, int> onSuccess,
            Action<Exception> onError)
        {
            const int aboveCount = 10;
            const int belowCount = 10;
            const int topLimitRankBuffer = 20;

            var topLimit = LeaderboardFetchLimit;
            var userScore = score;

            SubmitGlobalScore(userScore, () =>
            {
                GetPlayerRankGlobalLeaderboard((userRank, userScoreAfterSubmit) =>
                {
                    // The leaderboard may reject a score that is lower than the user's existing score.
                    userScore = userScoreAfterSubmit;

                    var playerId = GetPlayerId();
                    var rankOffset = Math.Max(1, userRank - aboveCount);

                    if (userRank > (long)topLimit + topLimitRankBuffer)
                    {
                        var fakeEntries = CreateFakeEntriesAroundUser(
                            userRank, userScore, playerId, aboveCount, belowCount);
                        onSuccess?.Invoke(fakeEntries, rankOffset);
                        return;
                    }

                    GetTopScores(topLimit, entries =>
                    {
                        // If the top leaderboard contains the user, return entries around the user.
                        // Otherwise, create fake entries and mix in available real entries near the top limit.
                        var userIndex = entries.FindIndex(entry => entry.UserId == playerId);
                        if (userIndex >= 0)
                        {
                            var startIndex = Math.Max(0, userIndex - aboveCount);
                            var endIndex = Math.Min(entries.Count, userIndex + belowCount + 1);
                            var resultEntries = entries.GetRange(startIndex, endIndex - startIndex);
                            onSuccess?.Invoke(resultEntries, startIndex + 1);
                            return;
                        }

                        var totalLeaderboardEntries = entries.Count;
                        var currentProfile = MocaLib.Instance.PlayerProfileManager.CurrentProfile;
                        var safePlayerId = playerId ?? "unknown_player";

                        // Boundary case: the user's surrounding range overlaps the fetched top entries.
                        if (userRank <= totalLeaderboardEntries + aboveCount)
                        {
                            var resultEntries = new List<LeaderboardEntry>(aboveCount + 1 + belowCount);

                            // Real ranks available from cached top list
                            // We need ranks [rank-10, rank-1], intersected with the fetched real entries.
                            var realAboveStartRank = Math.Max(1, userRank - aboveCount);
                            var realAboveEndRank = Math.Min(totalLeaderboardEntries, userRank - 1);

                            if (realAboveStartRank <= realAboveEndRank)
                            {
                                var startIndex = realAboveStartRank - 1; // rank -> 0-based index
                                var count = realAboveEndRank - realAboveStartRank + 1;
                                resultEntries.AddRange(entries.GetRange(startIndex, count));
                            }

                            // Fill the remaining ranks above the user with fake entries.
                            var fakeAboveStartRank = Math.Max(totalLeaderboardEntries + 1, userRank - aboveCount);
                            var fakeAboveEndRank = userRank - 1;

                            if (fakeAboveStartRank <= fakeAboveEndRank)
                            {
                                // Score boundary: from just below lastRealScore down to just above userScore
                                var highBound = resultEntries.Count > 0
                                    ? resultEntries[^1].Score - 1  // just below last real entry
                                    : userScore + (fakeAboveEndRank - fakeAboveStartRank + 2) * 10;
                                var lowBound = userScore + 1;
                                if (highBound < lowBound)
                                {
                                    // Degenerate case: user ties (or nearly ties) the bottom real entry.
                                    // Make all fake entries tie with it so scores stay monotonic by rank.
                                    highBound = lowBound = resultEntries[^1].Score;
                                }

                                var fakeCount = fakeAboveEndRank - fakeAboveStartRank + 1;

                                for (var fakeRank = fakeAboveStartRank; fakeRank <= fakeAboveEndRank; fakeRank++)
                                {
                                    // t = 1.0 at fakeAboveStartRank (closest to real, highest score)
                                    // t = 0.0 at fakeAboveEndRank (closest to user, lowest score)
                                    var t = fakeCount > 1
                                        ? (float)(fakeAboveEndRank - fakeRank) / (fakeCount - 1)
                                        : 0.5f;

                                    var interpolatedScore = (int)(lowBound + t * (highBound - lowBound));

                                    // Add jitter while keeping the score within [lowBound, highBound].
                                    var seed = HashCode.Combine(LeaderboardId.Global, safePlayerId, userRank, fakeRank);
                                    var maxJitter = Mathf.Max(0, (highBound - lowBound) / (fakeCount * 4));
                                    var jitter = maxJitter > 0 ? new Random(seed).Next(-maxJitter, maxJitter) : 0;
                                    var fakeScore = Mathf.Clamp(interpolatedScore + jitter, lowBound, highBound);

                                    resultEntries.Add(new LeaderboardEntry
                                    {
                                        UserId = $"sim_{LeaderboardId.Global}_{fakeRank}",
                                        Score = fakeScore,
                                        DisplayName = LeaderboardMangerHelper.GenerateFakeDisplayName(LeaderboardId.Global, fakeRank),
                                        Avatar = null,
                                        Metadata = new()
                                    });
                                }
                            }

                            // User entry
                            resultEntries.Add(LeaderboardMangerHelper.CreateFakeEntryAroundRank(LeaderboardId.Global, safePlayerId, userScore, userRank, userRank, currentProfile, isUser: true));

                            // Fake below ranks [rank+1 .. rank+10]
                            for (var fakeRank = userRank + 1; fakeRank <= userRank + belowCount; fakeRank++)
                            {
                                resultEntries.Add(LeaderboardMangerHelper.CreateFakeEntryAroundRank(LeaderboardId.Global, safePlayerId, userScore, fakeRank, userRank, currentProfile, isUser: false));
                            }

                            onSuccess?.Invoke(resultEntries, rankOffset);
                            return;
                        }

                        var fakeEntries = CreateFakeEntriesAroundUser(
                            userRank, userScore, playerId, aboveCount, belowCount);
                        onSuccess?.Invoke(fakeEntries, rankOffset);
                    }, onError);
                }, onError);
            }, onError);
        }

        private static List<LeaderboardEntry> CreateFakeEntriesAroundUser(
            int userRank,
            int userScore,
            string playerId,
            int aboveCount,
            int belowCount)
        {
            var entries = new List<LeaderboardEntry>(aboveCount + 1 + belowCount);
            var currentProfile = MocaLib.Instance.PlayerProfileManager.CurrentProfile;
            var safePlayerId = playerId ?? "unknown_player";

            for (var fakeRank = userRank - aboveCount; fakeRank < userRank; fakeRank++)
            {
                entries.Add(LeaderboardMangerHelper.CreateFakeEntryAroundRank(
                    LeaderboardId.Global, safePlayerId, userScore, fakeRank, userRank, currentProfile, isUser: false));
            }

            entries.Add(LeaderboardMangerHelper.CreateFakeEntryAroundRank(
                LeaderboardId.Global, safePlayerId, userScore, userRank, userRank, currentProfile, isUser: true));

            for (var fakeRank = userRank + 1; fakeRank <= userRank + belowCount; fakeRank++)
            {
                entries.Add(LeaderboardMangerHelper.CreateFakeEntryAroundRank(
                    LeaderboardId.Global, safePlayerId, userScore, fakeRank, userRank, currentProfile, isUser: false));
            }

            return entries;
        }

        private static void SubmitScore(string leaderboard, int score, Action onSuccess, Action<Exception> onError)
        {
            MocaLib.Instance.LeaderboardManager.SubmitScore(
                leaderboardName: leaderboard,
                scoreName: "score",
                newScore: score,
                onSuccess: onSuccess,
                onError: onError
            );
        }

        public static void GetTopScores(int topCount, Action<List<LeaderboardEntry>> onSuccess, Action<Exception> onError = null)
        {
            if ( LeaderboardCacheExpired() || _cachedLeaderboardEntries.Count < topCount)
            {
                GetTopScores(LeaderboardId.Global, topCount, entries =>
                {
                    _lastTimeGetTopLeaderboard = UnityEngine.Time.realtimeSinceStartup;
                    _cachedLeaderboardEntries = entries ?? new ();
                    onSuccess?.Invoke(new List<LeaderboardEntry>(_cachedLeaderboardEntries));
                }, onError);
            }
            else
            {
                var result = _cachedLeaderboardEntries.Count > topCount
                    ? _cachedLeaderboardEntries.GetRange(0, topCount)
                    : new List<LeaderboardEntry>(_cachedLeaderboardEntries);
                onSuccess?.Invoke(result);
            }
        }

        private static void GetTopScores(string leaderboard, int topCount, Action<List<LeaderboardEntry>> onSuccess,
            Action<Exception> onError = null)
        {
            MocaLib.Instance.LeaderboardManager.GetTopScores(leaderboard, "score", topCount, ScoreOrder.HigherIsBetter, onSuccess, onError);
        }

        public static void UpdatePlayerProfileGlobalLeaderboard(string displayName,
            string avatar,
            Dictionary<string, object> metadata,
            Action onSuccess = null,
            Action<Exception> onError = null)
        {
            // Update cache
            if (!LeaderboardCacheExpired())
            {
                var playerId = GetPlayerId();
                if (!string.IsNullOrEmpty(playerId))
                {
                    var existingEntry = _cachedLeaderboardEntries.Find(entry => entry.UserId == playerId);
                    if (existingEntry != null)
                    {
                        existingEntry.DisplayName = displayName;
                        existingEntry.Avatar = avatar;
                        existingEntry.Metadata = metadata;
                    }
                }
            }

            UpdatePlayerProfile(LeaderboardId.Global, displayName, avatar, metadata, onSuccess, onError);
        }

        private static void UpdatePlayerProfile(string leaderboard, string displayName,
            string avatar,
            Dictionary<string, object> metadata,
            Action onSuccess = null,
            Action<Exception> onError = null)
        {
            MocaLib.Instance.LeaderboardManager.UpdatePlayerProfile(leaderboard, "score", displayName, avatar, metadata, onSuccess, onError);
        }

        public static void GetPlayerRankGlobalLeaderboard(Action<int, int> onSuccess, Action<Exception> onError = null)
        {
            // Search from cache first
            if (!LeaderboardCacheExpired())
            {
                var playerId = GetPlayerId();
                if (!string.IsNullOrEmpty(playerId))
                {
                    var index = _cachedLeaderboardEntries.FindIndex(entry => entry.UserId == playerId);
                    if (index >= 0)
                    {
                        var entry = _cachedLeaderboardEntries[index];
                        onSuccess?.Invoke(index + 1, entry.Score);
                        return;
                    }
                }
            }

            GetPlayerRank(LeaderboardId.Global, onSuccess, onError);
        }

        private static void GetPlayerRank(string leaderboardName, Action<int, int> onSuccess, Action<Exception> onError = null)
        {
            MocaLib.Instance.LeaderboardManager.GetPlayerRank(leaderboardName, "score", ScoreOrder.HigherIsBetter, onSuccess, onError);
        }

        private static bool LeaderboardCacheExpired()
        {
            return _lastTimeGetTopLeaderboard < 0 || UnityEngine.Time.realtimeSinceStartup - _lastTimeGetTopLeaderboard > CacheExpiredSeconds;
        }

        public static string GetPlayerId()
        {
            return MocaLib.Instance.PlayerProfileManager?.CurrentProfile?.UserId;
        }

        public abstract class LeaderboardId
        {
            public const string Global = "global";
        }
    }

    public static class LeaderboardUtils
    {
        public static int GetLocalAvatarId(this LeaderboardEntry entry)
        {
            if (entry == null || entry.Avatar == null)
                return 0;

            var avatar = entry.Avatar;
            if (avatar.StartsWith("local:") && int.TryParse(avatar.Substring(6), out var id))
                return id;

            return 0;
        }

        public static int GetProfileBannerId(this LeaderboardEntry entry)
        {
            if (entry?.Metadata == null)
                return 0;

            if (entry.Metadata.TryGetValue(UserProfileKey.ProfileBannerImage, out var bannerId))
            {
                if (bannerId is int id
                    || (bannerId is string str && int.TryParse(str, out id)))
                    return id;
            }

            return 0;
        }

        public static int GetAvatarFrameId(this LeaderboardEntry entry)
        {
            if (entry?.Metadata == null)
                return 0;

            if (entry.Metadata.TryGetValue(UserProfileKey.AvatarFrame, out var frameId))
            {
                if (frameId is int id
                    || (frameId is string str && int.TryParse(str, out id)))
                    return id;
            }

            return 0;
        }
    }
}
