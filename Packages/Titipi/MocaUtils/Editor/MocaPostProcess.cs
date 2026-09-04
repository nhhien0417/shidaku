using System;

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
using System.IO;
using System.Net;
using System.Collections.Generic;
#endif

namespace Titipi.Editor
{
    public class MocaPostProcess : MonoBehaviour
    {
        private const string CLASS_TAG = "[[ Titipi.Editor.MocaPostProcess ]]";

        private static MocaBuildUtils _util = new();

        [PostProcessBuild(int.MaxValue)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string buildPath)
        {
            if (buildTarget != BuildTarget.iOS) return;

            _util.Log.Info($"{CLASS_TAG} OnPostprocessBuild: iOS");

            var pbxProj = buildPath + "/Unity-iPhone.xcodeproj/project.pbxproj";
            var plistPath = buildPath + "/Info.plist";
            var entitlementsPath = buildPath + "/Unity-iPhone/project.entitlements";

            AddFrameworks(pbxProj, "AdSupport.framework, iAd.framework, AppTrackingTransparency.framework, UserNotifications.framework");
            UpdateCapabilities(pbxProj, entitlementsPath);
            UpdateBuildSettings(pbxProj);
            UpdateProjectPlist(plistPath);
        }

        private static void UpdateBuildSettings(string projectPath)
        {
#if UNITY_IOS
                _util.Log.Info($"{CLASS_TAG} UpdateBuildSettings");

                PBXProject project = new PBXProject();
                project.ReadFromFile(projectPath);

                string mainTargetId = project.GetUnityMainTargetGuid();
                project.AddBuildProperty(mainTargetId, "OTHER_LDFLAGS", "-ObjC");
                project.SetBuildProperty(mainTargetId, "CLANG_ENABLE_MODULES", "YES");
                project.SetBuildProperty(mainTargetId, "GCC_ENABLE_OBJC_EXCEPTIONS", "YES");
                project.SetBuildProperty(mainTargetId, "ENABLE_BITCODE", "NO");
                project.SetBuildProperty(mainTargetId, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");

                string frameworkId = project.GetUnityFrameworkTargetGuid();
                project.SetBuildProperty(frameworkId, "ENABLE_BITCODE", "NO");
                project.SetBuildProperty(frameworkId, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");

                File.WriteAllText(projectPath, project.WriteToString());
#endif
        }

        private static void UpdateProjectPlist(string plistPath)
        {
#if UNITY_IOS
                _util.Log.Info($"{CLASS_TAG} UpdateProjectPlist");

                PlistDocument plist = new PlistDocument();
                plist.ReadFromString(File.ReadAllText(plistPath));

                // Get root
                PlistElementDict rootDict = plist.root;

                rootDict.SetBoolean("ITSAppUsesNonExemptEncryption", false);

                // remove exit on suspend if it exists
                // Ref: https://forum.unity.com/threads/the-info-plist-contains-a-key-uiapplicationexitsonsuspend.689200/
                var exitsOnSuspendKey = "UIApplicationExitsOnSuspend";
                if (rootDict.values.ContainsKey(exitsOnSuspendKey))
                {
                    rootDict.values.Remove(exitsOnSuspendKey);
                }

                rootDict.SetString("NSUserTrackingUsageDescription", "We reply on tracking to improve our game and to deliver personalized ads to you.");
                rootDict.SetString("FirebaseMessagingAutoInitEnabled", "NO");

                PlistElementArray UISupportedInterfaceOrientations = rootDict.values["UISupportedInterfaceOrientations"].AsArray();
                UISupportedInterfaceOrientations.AddString ("UIInterfaceOrientationPortraitUpsideDown");
                UISupportedInterfaceOrientations.AddString ("UIInterfaceOrientationLandscapeLeft");
                UISupportedInterfaceOrientations.AddString ("UIInterfaceOrientationLandscapeRight");

                File.WriteAllText(plistPath, plist.WriteToString());
#endif
        }

        private static void UpdateCapabilities(string projectPath, string entitlementPath)
        {
#if UNITY_IOS
                _util.Log.Info($"{CLASS_TAG} UpdateCapabilities");

                var scheme = "Unity-iPhone";
                var pcm = new ProjectCapabilityManager(projectPath, entitlementPath, scheme);

                pcm.AddBackgroundModes(BackgroundModesOptions.RemoteNotifications);
                pcm.AddPushNotifications(development: false);

                pcm.WriteToFile();
#endif
        }

        private static void AddFrameworks(string projectPath, string frameworks)
        {
#if UNITY_IOS
                _util.Log.Info($"{CLASS_TAG} AddFrameworks");

                if (frameworks == null || frameworks.Equals("none")) return;

                string[] iOSFrameworks = frameworks.Split(new char[] {' ', ','}, StringSplitOptions.RemoveEmptyEntries);

                if (iOSFrameworks.Length > 0)
                {
                    PBXProject project = new PBXProject();
                    project.ReadFromFile(projectPath);

#if UNITY_2019_3_OR_NEWER
                    string targetGuid = project.GetUnityFrameworkTargetGuid();
#else
                    string targetGUID = project.TargetGuidByName("Unity-iPhone");
#endif
                    foreach (var framework in iOSFrameworks)
                    {
                        project.AddFrameworkToProject(targetGuid, framework, false);
                    }

                    File.WriteAllText(projectPath, project.WriteToString());
                }
#endif
        }
    }
}
