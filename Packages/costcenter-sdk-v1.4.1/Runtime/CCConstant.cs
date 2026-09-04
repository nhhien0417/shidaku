using UnityEngine;

namespace CostCenter {
    public class CCConstant
    {
        private const string PLAYERPREFS_PREFIX = "CostCenter";
        internal static readonly string FIRST_OPEN_KEY = $"{PLAYERPREFS_PREFIX}_FirstOpen";
        internal static readonly string TRACKED_ATT_KEY = $"{PLAYERPREFS_PREFIX}_TrackedATT";
        internal static readonly string TRACKED_MMP_KEY = $"{PLAYERPREFS_PREFIX}_TrackedMMP";

        private static bool _isFirstOpen = false;
        internal static bool IsFirstOpen {
            get {
                if (!PlayerPrefs.HasKey(CCConstant.FIRST_OPEN_KEY)) {
                    _isFirstOpen = true;
                    PlayerPrefs.SetInt(CCConstant.FIRST_OPEN_KEY, 1);
                }
                return _isFirstOpen;
            }
        }
    }
}
