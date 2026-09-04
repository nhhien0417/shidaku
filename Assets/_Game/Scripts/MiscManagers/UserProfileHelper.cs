using _Game.Scripts.Common;
using Titipi.MocaLib.Runtime.Services;
using UnityEngine;
using UserDataPack;

public static class UserProfileHelper
{
    public static bool SyncUserProfile()
    {
        var userProfile = UserData.Instance.UserProfile;
        var profileManager = MocaLib.Instance.PlayerProfileManager;
        var isDirty = false;

        if (profileManager.CurrentProfile.DisplayName != userProfile.Username)
        {
            profileManager.SetDisplayName(userProfile.Username);
            isDirty = true;
        }

        var avatarIndex = userProfile.GetAvatarIndex();
        if (profileManager.GetLocalAvatarId() != avatarIndex)
        {
            profileManager.SetLocalAvatar(avatarIndex);
            isDirty = true;
        }

        var avatarFrame = profileManager.GetMetadataValue<string>(UserProfileKey.AvatarFrame);
        if (avatarFrame != userProfile.AvatarFrameId)
        {
            profileManager.SetMetadataValue(UserProfileKey.AvatarFrame, userProfile.AvatarFrameId);
            isDirty = true;
        }

        var bgimage = profileManager.GetMetadataValue<string>(UserProfileKey.ProfileBannerImage);
        if (bgimage != userProfile.BackgroundId)
        {
            profileManager.SetMetadataValue(UserProfileKey.ProfileBannerImage, userProfile.BackgroundId);
            isDirty = true;
        }

        if (isDirty)
        {
            profileManager.SaveProfile(() =>
            {
                var currentProfile = MocaLib.Instance.PlayerProfileManager.CurrentProfile;
                Game.Leaderboard.LeaderboardManager.UpdatePlayerProfileGlobalLeaderboard(currentProfile.DisplayName, currentProfile.Avatar, currentProfile.Metadata);
                Debug.Log("Player profile synced successfully.");

            }, exception =>
            {
                Debug.LogError($"Failed to sync player profile: {exception.Message}");
            });

            return true;
        }

        return false;
    }
}