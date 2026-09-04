#if MOCALIB_USE_FIREBASE_LEADERBOARD

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;

using Titipi.MocaLib.Runtime.Common;

namespace Titipi.MocaLib.Runtime.Services
{
    public enum ScoreOrder
    {
        HigherIsBetter,
        LowerIsBetter
    }

    public class LeaderboardEntry
    {
        public string UserId;
        public string DisplayName;
        public string Avatar;
        public int Score;
        public Dictionary<string, object> Metadata;
    }

    public class LeaderboardManager : MonoBehaviour
    {
        private const string TAG = "LeaderboardManager";

        // Firestore structure:
        // leaderboards/<leaderboardName>/scores/<userId>
        // userId will have a `scoreName` field
        public void SubmitScore(
            string leaderboardName,
            string scoreName,
            int newScore,
            ScoreOrder scoreOrder = ScoreOrder.HigherIsBetter,
            Action onSuccess = null,
            Action<Exception> onError = null)
        {
            var db = FirebaseFirestore.DefaultInstance;
            var auth = FirebaseAuth.DefaultInstance;

            if (auth.CurrentUser == null)
            {
                onError?.Invoke(
                    new Exception("FirebaseAuth.CurrentUser is null — possibly offline or not authenticated."));
                return;
            }

            var userId = auth.CurrentUser.UserId;
            var leaderboardRef = db
                .Collection("leaderboards")
                .Document(leaderboardName)
                .Collection("scores")
                .Document(userId);

            leaderboardRef.GetSnapshotAsync().ContinueWithOnMainThread(getTask =>
            {
                if (getTask.IsFaulted || getTask.IsCanceled)
                {
                    onError?.Invoke(getTask.Exception ?? new Exception("Failed to fetch Firestore document"));
                    return;
                }

                var snapshot = getTask.Result;

                bool existingScoreIsBetter = snapshot.Exists &&
                    snapshot.TryGetValue(scoreName, out object currentScoreObj) &&
                    currentScoreObj is long currentScore &&
                    (scoreOrder == ScoreOrder.HigherIsBetter ? currentScore >= newScore : currentScore <= newScore);

                if (existingScoreIsBetter)
                {
                    onSuccess?.Invoke();
                    return;
                }

                var profile = MocaLib.Instance.PlayerProfileManager?.CurrentProfile;
                if (profile == null)
                {
                    onError?.Invoke(new Exception("PlayerProfile is null"));
                    return;
                }

                var leaderboardEntry = new Dictionary<string, object>
                {
                    { "DisplayName", profile.DisplayName },
                    { "Avatar", profile.Avatar },
                    { scoreName, newScore },
                    { "UpdatedAt", Timestamp.GetCurrentTimestamp() },
                    {"Metadata", profile.Metadata }
                };

                if (!snapshot.Exists)
                {
                    leaderboardEntry["CreatedAt"] = Timestamp.GetCurrentTimestamp();
                }

                leaderboardRef.SetAsync(leaderboardEntry, SetOptions.MergeAll).ContinueWithOnMainThread(setTask =>
                {
                    if (setTask.IsFaulted || setTask.IsCanceled)
                    {
                        onError?.Invoke(setTask.Exception ?? new Exception("Failed to set Firestore document"));
                        return;
                    }

                    Utils.MocaLibLog(TAG, $"Submitted score {newScore} for user {userId} in {leaderboardName}/{scoreName}");

                    onSuccess?.Invoke();
                });
            });
        }

        public void GetTopScores(
            string leaderboardName,
            string scoreName,
            int topCount,
            ScoreOrder scoreOrder = ScoreOrder.HigherIsBetter,
            Action<List<LeaderboardEntry>> onSuccess = null,
            Action<Exception> onError = null)
        {
            var db = FirebaseFirestore.DefaultInstance;
            var scoresRef = db
                .Collection("leaderboards")
                .Document(leaderboardName)
                .Collection("scores");

            // Order by score (desc for higher-is-better, asc for lower-is-better), then UpdatedAt asc (earlier = higher rank on tie)
            var query = scoreOrder == ScoreOrder.HigherIsBetter
                ? scoresRef.OrderByDescending(scoreName)
                : scoresRef.OrderBy(scoreName);

            query
                .OrderBy("UpdatedAt")
                .Limit(topCount)
                .GetSnapshotAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        onError?.Invoke(task.Exception);
                        return;
                    }

                    try
                    {
                        var results = new List<LeaderboardEntry>();
                        var snapshot = task.Result;

                        foreach (var doc in snapshot.Documents)
                        {
                            var data = doc.ToDictionary();

                            data.TryGetValue("DisplayName", out var nameObj);
                            data.TryGetValue("Avatar", out var avatarObj);
                            data.TryGetValue(scoreName, out var scoreObj);
                            data.TryGetValue("Metadata", out var metadataObj);

                            var score = GetScoreFromObject(scoreObj);

                            results.Add(new LeaderboardEntry
                            {
                                UserId = doc.Id,
                                DisplayName = nameObj?.ToString() ?? "Noname",
                                Avatar = avatarObj?.ToString() ?? "local:0",
                                Score = score,
                                Metadata = metadataObj is (Dictionary<string, object> metadata) ? metadata : new ()
                            });
                        }

                        onSuccess?.Invoke(results);
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke(e);
                    }
                });
        }

        public void UpdatePlayerProfile(
            string leaderboardName,
            string scoreName,
            string displayName,
            string avatar,
            Dictionary<string, object> metadata,
            Action onSuccess = null,
            Action<Exception> onError = null)
        {
            var db = FirebaseFirestore.DefaultInstance;
            var auth = FirebaseAuth.DefaultInstance;
            var userId = auth.CurrentUser?.UserId;

            if (string.IsNullOrEmpty(userId))
            {
                onError?.Invoke(new Exception("User is not logged in."));
                return;
            }

            var scoreDocRef = db
                .Collection("leaderboards")
                .Document(leaderboardName)
                .Collection("scores")
                .Document(userId);

            var updateData = new Dictionary<string, object>
            {
                { "DisplayName", displayName },
                { "Avatar", avatar },
                { "UpdatedAt", Timestamp.GetCurrentTimestamp() },
                { "Metadata", metadata }
            };

            scoreDocRef.SetAsync(updateData, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    onError?.Invoke(task.Exception);
                    return;
                }

                Utils.MocaLibLog(TAG, $"Updated profile for user {userId} in leaderboard '{leaderboardName}/{scoreName}'");

                onSuccess?.Invoke();
            });
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="leaderboardName"></param>
        /// <param name="scoreName"></param>
        /// <param name="onSuccess">First param is Rank, second is Score</param>
        /// <param name="onError"></param>
        public void GetPlayerRank(
            string leaderboardName,
            string scoreName,
            ScoreOrder scoreOrder = ScoreOrder.HigherIsBetter,
            Action<int, int> onSuccess = null,
            Action<Exception> onError = null)
        {
            var db = FirebaseFirestore.DefaultInstance;
            var scoresRef = db
                .Collection("leaderboards")
                .Document(leaderboardName)
                .Collection("scores");

            // Get current user ID from MocaLib profile
            var userId = MocaLib.Instance.PlayerProfileManager?.CurrentProfile?.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                onError?.Invoke(new Exception("UserId is null or empty."));
                return;
            }

            // Step 1️⃣: Get player's document to fetch UpdatedAt timestamp
            scoresRef.Document(userId).GetSnapshotAsync().ContinueWithOnMainThread(playerTask =>
            {
                if (playerTask.IsFaulted || playerTask.IsCanceled)
                {
                    onError?.Invoke(playerTask.Exception);
                    return;
                }

                var playerDoc = playerTask.Result;
                if (!playerDoc.Exists)
                {
                    onError?.Invoke(new Exception($"Player document not found for UserId: {userId}"));
                    return;
                }

                var data = playerDoc.ToDictionary();
                if (!data.TryGetValue("UpdatedAt", out var updatedAtObj) || !(updatedAtObj is Timestamp playerUpdatedAt))
                {
                    onError?.Invoke(new Exception("Player document missing UpdatedAt field."));
                    return;
                }
                data.TryGetValue(scoreName, out var scoreObj);
                var userScore = GetScoreFromObject(scoreObj);

                // Step 2️⃣: Count players with a better score (higher or lower depending on scoreOrder)
                var higherAgg = (scoreOrder == ScoreOrder.HigherIsBetter
                    ? scoresRef.WhereGreaterThan(scoreName, userScore)
                    : scoresRef.WhereLessThan(scoreName, userScore)).Count;
                higherAgg.GetSnapshotAsync(AggregateSource.Server).ContinueWithOnMainThread(higherTask =>
                {
                    if (higherTask.IsFaulted || higherTask.IsCanceled)
                    {
                        onError?.Invoke(higherTask.Exception);
                        return;
                    }

                    int higherCount = (int)higherTask.Result.Count;

                    // Step 3️⃣: Count players with same score but earlier UpdatedAt
                    var equalEarlierAgg = scoresRef
                        .WhereEqualTo(scoreName, userScore)
                        .WhereLessThan("UpdatedAt", playerUpdatedAt)
                        .Count;

                    equalEarlierAgg.GetSnapshotAsync(AggregateSource.Server).ContinueWithOnMainThread(equalTask =>
                    {
                        if (equalTask.IsFaulted || equalTask.IsCanceled)
                        {
                            onError?.Invoke(equalTask.Exception);
                            return;
                        }

                        int equalEarlierCount = (int)equalTask.Result.Count;
                        int rank = higherCount + equalEarlierCount + 1;

                        Utils.MocaLibLog(TAG, $"Player rank: {rank} (higher={higherCount}, equalEarlier={equalEarlierCount})");

                        onSuccess?.Invoke(rank, userScore);
                    });
                });
            });
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="leaderboardName"></param>
        /// <param name="scoreName"></param>
        /// <param name="score"></param>
        /// <param name="onSuccess">The return value is Rank at score given</param>
        /// <param name="onError"></param>
        public void GetRankAtScore(
            string leaderboardName,
            string scoreName,
            string score,
            ScoreOrder scoreOrder = ScoreOrder.HigherIsBetter,
            Action<int> onSuccess = null,
            Action<Exception> onError = null)
        {
            var db = FirebaseFirestore.DefaultInstance;
            var scoresRef = db
                .Collection("leaderboards")
                .Document(leaderboardName)
                .Collection("scores");

            // Get current user ID from MocaLib profile
            var userId = MocaLib.Instance.PlayerProfileManager?.CurrentProfile?.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                onError?.Invoke(new Exception("UserId is null or empty."));
                return;
            }

            // Step 1: Count players with a better score (higher or lower depending on scoreOrder)
            var higherAgg = (scoreOrder == ScoreOrder.HigherIsBetter
                ? scoresRef.WhereGreaterThan(scoreName, score)
                : scoresRef.WhereLessThan(scoreName, score)).Count;
            higherAgg.GetSnapshotAsync(AggregateSource.Server).ContinueWithOnMainThread(higherTask =>
            {
                if (higherTask.IsFaulted || higherTask.IsCanceled)
                {
                    onError?.Invoke(higherTask.Exception);
                    return;
                }

                int higherCount = (int)higherTask.Result.Count;

                // Step 2: Count players with same score but earlier UpdatedAt
                var equalEarlierAgg = scoresRef
                    .WhereEqualTo(scoreName, score)
                    .WhereLessThan("UpdatedAt", Timestamp.GetCurrentTimestamp())
                    .Count;

                equalEarlierAgg.GetSnapshotAsync(AggregateSource.Server).ContinueWithOnMainThread(equalTask =>
                {
                    if (equalTask.IsFaulted || equalTask.IsCanceled)
                    {
                        onError?.Invoke(equalTask.Exception);
                        return;
                    }

                    int equalEarlierCount = (int)equalTask.Result.Count;
                    int rank = higherCount + equalEarlierCount + 1;

                    Utils.MocaLibLog(TAG, $"Rank at score {score}: {rank} (higher={higherCount}, equalEarlier={equalEarlierCount})");

                    onSuccess?.Invoke(rank);
                });
            });
        }

        private int GetScoreFromObject(object obj)
        {
            var score = 0;
            if (obj is long l) score = (int)l;
            else if (obj is int i) score = i;
            else if (obj is double d) score = (int)d;
            else if (obj != null) int.TryParse(obj.ToString(), out score);

            return score;
        }
    }
}

#endif
