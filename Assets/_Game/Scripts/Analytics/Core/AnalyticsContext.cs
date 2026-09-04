using System.IO;
using UnityEngine.SceneManagement;
using UserDataPack;

namespace Analytics
{
    public static class AnalyticsContext
    {
        private static string _currentPlayMode = "";
        public static string CurrentPlayMode
        {
            get
            {
                var gameplaySceneName = Path.GetFileName(Key.GAMEPLAY_SCENE);
                return SceneManager.GetActiveScene().name == gameplaySceneName ? _currentPlayMode : "";
            }
            set => _currentPlayMode = value;
        }

        public static string CurrentPuzzleId { get; set; }
        public static int CurrentLevel { get; set; } = UserData.Instance.GameplayData.CurrentGameplayLevel;
    }
}
