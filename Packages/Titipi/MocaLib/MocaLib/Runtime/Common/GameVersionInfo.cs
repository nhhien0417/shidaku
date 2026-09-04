using UnityEngine;

namespace Titipi.MocaLib.Runtime.Common
{
    public class GameVersionInfo : MonoBehaviour
    {
        public string BuildVersion = "";
        public string BuildNumber = "";
        public string BuildTime = "";

        public static string BUILD_VERSION => buildVersion;
        public static string BUILD_NUMBER => buildNumber;
        public static string BUILD_TIME => buildTime;

        private void Start()
        {
            if (string.IsNullOrEmpty(buildVersion))
            {
                buildVersion = BuildVersion;
            }
            if (string.IsNullOrEmpty(buildNumber))
            {
                buildNumber = BuildNumber;
            }
            if (string.IsNullOrEmpty(buildTime))
            {
                buildTime = BuildTime;
            }
        }

        private static string buildVersion = "";
        private static string buildNumber = "";
        private static string buildTime = "";
    }
}
