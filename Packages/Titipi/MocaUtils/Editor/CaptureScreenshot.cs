#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Titipi.Editor
{
    public class CaptureScreenshot
    {
        [MenuItem("Tools/Screenshot/Take screenshot #S")]
        private static void Screenshot()
        {
            long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string saveFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), unixTime.ToString() + ".png");
            ScreenCapture.CaptureScreenshot(saveFile);
        }
    }
}

#endif
