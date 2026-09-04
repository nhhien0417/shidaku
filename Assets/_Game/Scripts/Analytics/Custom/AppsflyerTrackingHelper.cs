using System.Collections.Generic;
using Titipi.MocaLib.Runtime.Services;

namespace Analytics
{
    public class AppsflyerTrackingHelper
    {
        private static Dictionary<int, string> _completeLevelsToTrack = new()
        {
            { 1, "complete_level_1" },
            { 3, "complete_level_3" },
            { 5, "complete_level_5" },
            { 8, "complete_level_8" },
            { 11, "complete_level_11" },
            { 15, "complete_level_15" },
            { 20, "complete_level_20" },
            { 25, "complete_level_25" },
            { 30, "complete_level_30" },
            { 35, "complete_level_35" },
            { 40, "complete_level_40" },
            { 45, "complete_level_45" },
            { 50, "complete_level_50" },
            { 60, "complete_level_60" },
            { 70, "complete_level_70" },
            { 80, "complete_level_80" },
            { 90, "complete_level_90" },
            { 100, "complete_level_100" },
            { 115, "complete_level_115" },
            { 130, "complete_level_130" },
            { 145, "complete_level_145" },
            { 160, "complete_level_160" },
            { 175, "complete_level_175" },
            { 190, "complete_level_190" },
            { 210, "complete_level_210" },
            { 230, "complete_level_230" },
            { 250, "complete_level_250" },
            { 280, "complete_level_280" },
            { 310, "complete_level_310" },
            { 350, "complete_level_350" },
            { 400, "complete_level_400" },
            { 500, "complete_level_500" },
            { 550, "complete_level_550" },
            { 600, "complete_level_600" },
            { 650, "complete_level_650" },
            { 700, "complete_level_700" },
            { 800, "complete_level_800" },
            { 900, "complete_level_900" },
            { 1000, "complete_level_1000" },
            { 1100, "complete_level_1100" },
            { 1200, "complete_level_1200" },
            { 1350, "complete_level_1350" },
            { 1500, "complete_level_1500" },
            { 1700, "complete_level_1700" },
            { 1900, "complete_level_1900" },
            { 2100, "complete_level_2100" },
            { 2300, "complete_level_2300" },
            { 2500, "complete_level_2500" },
            { 3000, "complete_level_3000" }
        };

        public static void TrackFirstTimeCompleteLevel(int level, string playMode)
        {
            if (playMode == PlayMode.Normal)
            {
                var mmpManager = MocaLib.Instance.MMPManager;
                if (mmpManager != null)
                {
                    mmpManager.LogEvent("af_level_achieved");

                    if (_completeLevelsToTrack.TryGetValue(level, out string eventName))
                    {
                        mmpManager.LogEvent(eventName);
                    }
                }
            }
        }
    }
}