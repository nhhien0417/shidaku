#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

using UnityEditor.Android;
using Unity.Android.Types;
using AndroidArchitecture = UnityEditor.AndroidArchitecture;

namespace Titipi.Editor
{
    class MocaBuild
    {
        private const string CLASS_TAG = "[[ Titipi.Editor.MocaBuild ]]";

        public static void Build()
        {
            int oldBuildNumber = 0;
            int newBuildNumber = GetBuildNumberFromEnv();

            var util = new MocaBuildUtils();
            util.Log.Info($"{CLASS_TAG} Build script inputs:");
            util.PrintInputs();

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = util.GetActiveScenes(),
                locationPathName = util.Inputs.BuildOutput
            };

            var oldVersionNumber = PlayerSettings.bundleVersion;
            var newVersionNumber = GetVersionNumberFromEnv();
            if (!string.IsNullOrEmpty(newVersionNumber) && !string.Equals(newVersionNumber, "-1"))
            {
                PlayerSettings.bundleVersion = newVersionNumber;
            }

            if (util.Inputs.BuildPlatform == MocaBuildUtils.BuildPlatform.ANDROID)
            {
                buildPlayerOptions.target = BuildTarget.Android;
                buildPlayerOptions.options = BuildOptions.None;

                if (newBuildNumber > 0)
                {
                    oldBuildNumber = PlayerSettings.Android.bundleVersionCode;
                    PlayerSettings.Android.bundleVersionCode = newBuildNumber;
                }

                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = util.Inputs.AndroidKeystorePath;
                PlayerSettings.Android.keystorePass = util.Inputs.AndroidKeystorePassword;
                PlayerSettings.Android.keyaliasName = util.Inputs.AndroidKeystoreAlias;
                PlayerSettings.Android.keyaliasPass = util.Inputs.AndroidKeystoreAliasPassword;

                var originalArchitectures = PlayerSettings.Android.targetArchitectures;

                if (Environment.GetEnvironmentVariable("MOCA_BUILD_AAB") == "yes")
                {
                    EditorUserBuildSettings.buildAppBundle = true;
                }
                else
                {
                    EditorUserBuildSettings.buildAppBundle = false;
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;;
                }

                UserBuildSettings.DebugSymbols.level = DebugSymbolLevel.SymbolTable;
            }
            else if (util.Inputs.BuildPlatform == MocaBuildUtils.BuildPlatform.IOS)
            {
                buildPlayerOptions.target = BuildTarget.iOS;

                if (newBuildNumber > 0)
                {
                    oldBuildNumber = Int32.Parse(PlayerSettings.iOS.buildNumber);
                    PlayerSettings.iOS.buildNumber = newBuildNumber.ToString();
                }
            }
            else
            {
                util.Log.Fail($"{CLASS_TAG} Invalid buildPlatform: {util.Inputs.BuildPlatform}");
                EditorApplication.Exit(1);
            }

            BuildReport report = null;
            try
            {
                report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            }
            finally
            {
                if (!string.IsNullOrEmpty(newVersionNumber) && !string.Equals(newVersionNumber, "-1"))
                {
                    RestoreVersionNumber(oldVersionNumber);
                }

                if (newBuildNumber > 0)
                {
                    RestoreBuildNumber(util.Inputs.BuildPlatform, oldBuildNumber);
                }

                // Reset AAB option
                EditorUserBuildSettings.buildAppBundle = false;

                if (report != null)
                {
                    BuildSummary summary = report.summary;

                    if (summary.result == BuildResult.Succeeded)
                    {
                        util.Log.Done($"{CLASS_TAG} Unity build succeeded.");
                        EditorApplication.Exit(0);
                    }
                    else if (summary.result == BuildResult.Failed)
                    {
                        util.Log.Fail($"{CLASS_TAG} Unity build failed.");
                        EditorApplication.Exit(1);
                    }
                }
            }
        }

        private static int GetBuildNumberFromEnv()
        {
            var buildNumber = Environment.GetEnvironmentVariable("MOCA_BUILD_NUMBER");

            if (!string.IsNullOrEmpty(buildNumber))
            {
                return Int32.Parse(buildNumber);
            }

            return -1;
        }

        private static void RestoreBuildNumber(MocaBuildUtils.BuildPlatform platform, int buildNumber)
        {
            if (platform == MocaBuildUtils.BuildPlatform.ANDROID)
            {
                PlayerSettings.Android.bundleVersionCode = buildNumber;
            }
            else if (platform == MocaBuildUtils.BuildPlatform.IOS)
            {
                PlayerSettings.iOS.buildNumber = buildNumber.ToString();
            }
        }

        private static string GetVersionNumberFromEnv()
        {
            var versionNumber = Environment.GetEnvironmentVariable("MOCA_VERSION_NUMBER");

            return versionNumber;
        }

        private static void RestoreVersionNumber(string version)
        {
            PlayerSettings.bundleVersion = version;
        }
    }
}

#endif // UNITY_EDITOR
