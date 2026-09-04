using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

using Titipi.MocaLib.Runtime.Common;

namespace Titipi.MocaLib.Editor
{
    public class GameVersionProcess
    {
        [PostProcessScene(1)]
        public static void OnPostprocessScene()
        {
            var go = new GameObject("GameVersion")
            {
                transform =
                {
                    position = new Vector3()
                }
            };

            var vm = go.AddComponent<GameVersionInfo>();
            vm.BuildTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss \"GMT\"zzz");
            vm.BuildVersion = PlayerSettings.bundleVersion;
#if UNITY_ANDROID
            vm.BuildNumber = PlayerSettings.Android.bundleVersionCode.ToString();
#elif UNITY_IOS
            vm.BuildNumber = PlayerSettings.iOS.buildNumber;
#endif
        }
    }
}
