using System;
using System.Collections.Generic;
using Titipi.MocaLib.Runtime.Services;
using UnityEngine;

namespace UserDataPack.PropertyDataGroup
{
    [Serializable]
    public class UserProfile : IUserDataPropertyDataGroup
    {
        public string Username;
        public string AvatarId;
        public string AvatarFrameId;
        public string BackgroundId;
        public int CurrentRankLevel;
        public int LeaderboardPoint;

        public bool HasSubmitLeaderboardPoint;

        public void FixData()
        {
            if (string.IsNullOrEmpty(Username))
            {
                Username = $"Player_{Guid.NewGuid().ToString("N")[..8]}";
            }

            if (string.IsNullOrEmpty(AvatarId))
            {
                AvatarId = "0";
            }

            if (string.IsNullOrEmpty(AvatarFrameId))
            {
                AvatarFrameId = "0";
            }

            if (string.IsNullOrEmpty(BackgroundId))
            {
                BackgroundId = "0";
            }
        }

        public int GetAvatarIndex()
        {
            return int.TryParse(AvatarId, out var avatarIndex) ? avatarIndex : 0;
        }

        public int GetAvatarFrameIndex()
        {
            return int.TryParse(AvatarFrameId, out var avatarFrameIndex) ? avatarFrameIndex : 0;
        }

        public int GetBannerIndex()
        {
            return int.TryParse(BackgroundId, out var bannerIndex) ? bannerIndex : 0;
        }

        public void SubmitLeaderboardScoreAndFetch(int pointsToAdd = 0, Action<List<LeaderboardEntry>, int> onSuccess = null, Action<Exception> onError = null)
        {
            if (pointsToAdd > 0)
            {
                LeaderboardPoint += pointsToAdd;
            }

            Game.Leaderboard.LeaderboardManager.SubmitGlobalScore(LeaderboardPoint, (entries, rankOffset) =>
            {
                var playerId = Game.Leaderboard.LeaderboardManager.GetPlayerId();
                var userIndex = entries.FindIndex(entry => entry.UserId == playerId);
                if (userIndex >= 0)
                {
                    CurrentRankLevel = rankOffset + userIndex;
                }

                onSuccess?.Invoke(entries, rankOffset);
            }, onError);
        }

        public void SubmitLeaderboardScore(Action onSuccess = null, Action<Exception> onError = null)
        {
            Game.Leaderboard.LeaderboardManager.SubmitGlobalScore(LeaderboardPoint, onSuccess, onError);
        }
    }
}
