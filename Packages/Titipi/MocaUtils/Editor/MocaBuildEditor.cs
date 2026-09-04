using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Networking;

public static class MocaBuildEditor
{
    private const int Shift = 16;
    private const string AndroidOutputFolder = "_build/Android";
    private const string XcodeOutputFolder = "_build/XCode";
    internal const string SettingsTitle = "Moca Build Settings";

    internal static class LocalSettings
    {
        private const string Prefix = "Titipi.MocaBuild.";
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".titipi",
            "mocabuild-settings.json"
        );
        private static string sessionMocaVersionNumber = string.Empty;
        private static string sessionMocaBuildNumber = string.Empty;

        internal const string TitipiApiKey = "TITIPI_API_KEY";
        internal const string TitipiApiBaseUrl = "TITIPI_API_BASE_URL";
        internal const string MocaVersionNumber = "MOCA_VERSION_NUMBER";
        internal const string MocaBuildNumber = "MOCA_BUILD_NUMBER";
        [Serializable]
        private class SettingsData
        {
            public string titipiApiKey;
            public string titipiApiBaseUrl;
        }

        internal static string Get(string variableName)
        {
            if (variableName == MocaVersionNumber)
            {
                return !string.IsNullOrWhiteSpace(sessionMocaVersionNumber)
                    ? sessionMocaVersionNumber
                    : Environment.GetEnvironmentVariable(variableName);
            }

            if (variableName == MocaBuildNumber)
            {
                return !string.IsNullOrWhiteSpace(sessionMocaBuildNumber)
                    ? sessionMocaBuildNumber
                    : Environment.GetEnvironmentVariable(variableName);
            }

            var localOverride = GetLocalOverride(variableName);
            if (!string.IsNullOrWhiteSpace(localOverride))
            {
                return localOverride;
            }

            return Environment.GetEnvironmentVariable(variableName);
        }

        internal static string GetLocalOverride(string variableName)
        {
            var data = LoadData();
            switch (variableName)
            {
                case TitipiApiKey:
                    return data.titipiApiKey ?? string.Empty;
                case TitipiApiBaseUrl:
                    return data.titipiApiBaseUrl ?? string.Empty;
                case MocaVersionNumber:
                    return sessionMocaVersionNumber ?? string.Empty;
                case MocaBuildNumber:
                    return sessionMocaBuildNumber ?? string.Empty;
                default:
                    return string.Empty;
            }
        }

        internal static void SetLocalOverride(string variableName, string value)
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

            if (variableName == MocaVersionNumber)
            {
                sessionMocaVersionNumber = normalized;
                Environment.SetEnvironmentVariable(
                    MocaVersionNumber,
                    string.IsNullOrEmpty(normalized) ? null : normalized,
                    EnvironmentVariableTarget.Process
                );
                return;
            }

            if (variableName == MocaBuildNumber)
            {
                sessionMocaBuildNumber = normalized;
                Environment.SetEnvironmentVariable(
                    MocaBuildNumber,
                    string.IsNullOrEmpty(normalized) ? null : normalized,
                    EnvironmentVariableTarget.Process
                );
                return;
            }

            var data = LoadData();

            switch (variableName)
            {
                case TitipiApiKey:
                    data.titipiApiKey = normalized;
                    break;
                case TitipiApiBaseUrl:
                    data.titipiApiBaseUrl = normalized;
                    break;
            }

            SaveData(data);
        }

        internal static void ClearAll()
        {
            SaveData(new SettingsData());
        }

        internal static void ClearVersionOverrides()
        {
            sessionMocaVersionNumber = string.Empty;
            sessionMocaBuildNumber = string.Empty;
            Environment.SetEnvironmentVariable(MocaVersionNumber, null, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(MocaBuildNumber, null, EnvironmentVariableTarget.Process);
        }

        private static SettingsData LoadData()
        {
            MigrateFromEditorPrefsIfNeeded();

            if (!File.Exists(SettingsPath))
            {
                return new SettingsData();
            }

            try
            {
                var json = File.ReadAllText(SettingsPath, Encoding.UTF8);
                var parsed = JsonUtility.FromJson<SettingsData>(json);
                return parsed ?? new SettingsData();
            }
            catch
            {
                return new SettingsData();
            }
        }

        private static void SaveData(SettingsData data)
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SettingsPath, json, Encoding.UTF8);
        }

        private static void MigrateFromEditorPrefsIfNeeded()
        {
            if (File.Exists(SettingsPath))
            {
                return;
            }

            var data = new SettingsData
            {
                titipiApiKey = EditorPrefs.GetString(Prefix + TitipiApiKey, string.Empty),
                titipiApiBaseUrl = EditorPrefs.GetString(Prefix + TitipiApiBaseUrl, string.Empty)
            };

            if (
                !string.IsNullOrWhiteSpace(data.titipiApiKey) ||
                !string.IsNullOrWhiteSpace(data.titipiApiBaseUrl)
            )
            {
                SaveData(data);
            }
        }
    }

    [Serializable]
    private class VersionResponse
    {
        public string platform;
        public string channel;
        public string appid;
        public string version;
        public int build;
    }

    [Serializable]
    private class KeystoreResponse
    {
        public string appid;
        public string keystore_password;
        public string key_alias;
        public string key_password;
    }

    [MenuItem("Tools/MocaBuild/1. Build APK")]
    public static void BuildApkMenu()
    {
        ExecuteWithTiming(() =>
        {
            BuildSignedApk(openOutputFolder: true);
        });
    }

    [MenuItem("Tools/MocaBuild/2. Build AAB")]
    public static void BuildAabMenu()
    {
        ExecuteWithTiming(() =>
        {
            BuildSignedAab(openOutputFolder: true);
        });
    }

    [MenuItem("Tools/MocaBuild/3. Export to XCode Project")]
    public static void ExportToXcodeMenu()
    {
        ExecuteWithTiming(() =>
        {
            ExportToXcode(channel: "xcode", openOutputFolder: true);
        });
    }

    [MenuItem("Tools/MocaBuild/4. Clean _build folder + XCode temp files")]
    public static void CleanUpMenu()
    {
        ExecuteWithTiming(() =>
        {
            DeleteBuildFolder();
            CleanupXcodeTempFiles();
        });
    }

    [MenuItem("Tools/MocaBuild/5. Settings")]
    public static void OpenSettingsWindow()
    {
        MocaBuildSettingsWindow.Open();
    }

    private static void ExecuteWithTiming(Action action)
    {
        var start = DateTime.UtcNow;
        try
        {
            action();
            var elapsed = (int)(DateTime.UtcNow - start).TotalSeconds;
            UnityEngine.Debug.Log($"MOCABUILD: Elapsed time: {StringOfSeconds(elapsed)}");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"MOCABUILD ERROR: {ex.Message}");
            throw;
        }
    }

    private static (string productName, string versionNumber, string buildNumber, string binaryName) BuildSignedApk(bool openOutputFolder)
    {
        return BuildAndroid(
            binaryFormat: "APK",
            openOutputFolder: openOutputFolder,
            developmentBuild: false,
            sanitizeProductNameInBinary: true,
            restoreVersionAfterBuild: true
        );
    }

    private static (string productName, string versionNumber, string buildNumber, string binaryName) BuildSignedAab(bool openOutputFolder)
    {
        return BuildAndroid(
            binaryFormat: "AAB",
            openOutputFolder: openOutputFolder,
            developmentBuild: false,
            sanitizeProductNameInBinary: true,
            restoreVersionAfterBuild: true
        );
    }

    private static (
        string productName,
        string versionNumber,
        string buildNumber,
        string binaryName
    ) BuildAndroid(
        string binaryFormat,
        bool openOutputFolder,
        bool developmentBuild,
        bool sanitizeProductNameInBinary = false,
        bool restoreVersionAfterBuild = false
    )
    {
        var appId = GetApplicationIdentifier(BuildTargetGroup.Android);
        var productName = PlayerSettings.productName;
        EnsureNonEmpty(appId, "Cannot retrieve Android applicationIdentifier");
        EnsureNonEmpty(productName, "Cannot retrieve productName");

        DeletePathIfExists(AndroidOutputFolder);
        Directory.CreateDirectory(AndroidOutputFolder);

        var originalBundleVersion = PlayerSettings.bundleVersion;
        var originalBuildNumber = PlayerSettings.Android.bundleVersionCode;

        var channel = binaryFormat == "APK" ? "apk" : "googleplay";
        var (versionNumber, buildNumber) = GetGameVersion("android", channel, appId);
        ApplyVersionToPlayerSettings(versionNumber, buildNumber, BuildTargetGroup.Android);

        var (keystorePassword, keyAlias, keyPassword) = GetKeystoreInfo(appId);
        ConfigureAndroidKeystore(keystorePassword, keyAlias, keyPassword);

        var extension = binaryFormat == "APK" ? "apk" : "aab";
        var binaryProductName = sanitizeProductNameInBinary ? SanitizeAlphaNumeric(productName) : productName;
        var binaryName = BinaryFileName(binaryProductName, versionNumber, extension);
        var outputPath = Path.Combine(AndroidOutputFolder, binaryName);

        PrintSummary(productName, "Android", appId, versionNumber, buildNumber);

        var originalBundleFlag = EditorUserBuildSettings.buildAppBundle;

        var originalArchitectures = PlayerSettings.Android.targetArchitectures;
        try
        {
            EditorUserBuildSettings.buildAppBundle = binaryFormat == "AAB";
            if (binaryFormat == "APK")
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            }
            BuildWithUnity(
                BuildTarget.Android,
                outputPath,
                developmentBuild ? BuildOptions.Development : BuildOptions.None
            );
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = originalBundleFlag;
            PlayerSettings.Android.targetArchitectures = originalArchitectures;
            if (restoreVersionAfterBuild)
            {
                PlayerSettings.bundleVersion = originalBundleVersion;
                PlayerSettings.Android.bundleVersionCode = originalBuildNumber;
                AssetDatabase.SaveAssets();
            }
        }

        UnityEngine.Debug.Log($"MOCABUILD: Unity build succeeded. `{binaryName}` is in {AndroidOutputFolder}");
        if (openOutputFolder)
        {
            EditorUtility.RevealInFinder(Path.GetFullPath(AndroidOutputFolder));
        }

        return (productName, versionNumber, buildNumber, binaryName);
    }

    private static (string productName, string versionNumber, string buildNumber) ExportToXcode(string channel, bool openOutputFolder)
    {
        if (Application.platform != RuntimePlatform.OSXEditor)
        {
            throw new InvalidOperationException("iOS build/export is only supported on macOS.");
        }

        var appId = GetApplicationIdentifier(BuildTargetGroup.iOS);
        var productName = PlayerSettings.productName;
        EnsureNonEmpty(appId, "Cannot retrieve iOS applicationIdentifier");
        EnsureNonEmpty(productName, "Cannot retrieve productName");

        DeletePathIfExists(XcodeOutputFolder);
        var targetRoot = Path.Combine(XcodeOutputFolder, productName);
        Directory.CreateDirectory(targetRoot);

        var (versionNumber, buildNumber) = GetGameVersion("ios", channel, appId);
        ApplyVersionToPlayerSettings(versionNumber, buildNumber, BuildTargetGroup.iOS);

        PrintSummary(productName, "iOS", appId, versionNumber, buildNumber);

        BuildWithUnity(BuildTarget.iOS, targetRoot, BuildOptions.None);

        var workspace = Path.Combine(targetRoot, "Unity-iPhone.xcworkspace");
        if (!Directory.Exists(workspace) && !File.Exists(workspace))
        {
            throw new InvalidOperationException(
                "Unity failed to generate `Unity-iPhone.xcworkspace`. Check CocoaPods/pod install output in the console."
            );
        }

        UnityEngine.Debug.Log($"MOCABUILD: Unity build succeeded. Resulting XCode project is in {XcodeOutputFolder}");
        if (openOutputFolder)
        {
            EditorUtility.RevealInFinder(Path.GetFullPath(targetRoot));
        }

        return (productName, versionNumber, buildNumber);
    }

    private static void BuildWithUnity(BuildTarget target, string locationPath, BuildOptions extraOptions)
    {
        var scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes in Build Settings.");
        }

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            target = target,
            locationPathName = locationPath,
            options = extraOptions
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"Unity build failed: {report.summary.result}");
        }
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenes.Add(scene.path);
            }
        }

        return scenes.ToArray();
    }

    private static void ConfigureAndroidKeystore(string keystorePassword, string keyAlias, string keyPassword)
    {
        var keystorePath = Path.GetFullPath("upload.jks");
        if (!File.Exists(keystorePath))
        {
            throw new FileNotFoundException($"File not found: {keystorePath}");
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystorePath;
        PlayerSettings.Android.keystorePass = keystorePassword;
        PlayerSettings.Android.keyaliasName = keyAlias;
        PlayerSettings.Android.keyaliasPass = keyPassword;
    }

    private static void ApplyVersionToPlayerSettings(string version, string build, BuildTargetGroup group)
    {
        PlayerSettings.bundleVersion = version;

        if (group == BuildTargetGroup.Android)
        {
            if (!int.TryParse(build, out var code))
            {
                throw new InvalidOperationException($"Android build number must be an integer. Received: {build}");
            }

            PlayerSettings.Android.bundleVersionCode = code;
        }
        else if (group == BuildTargetGroup.iOS)
        {
            PlayerSettings.iOS.buildNumber = build;
        }
    }

    private static string GetApplicationIdentifier(BuildTargetGroup group)
    {
#if UNITY_2021_2_OR_NEWER
        var editorAssembly = typeof(UnityEditor.Editor).Assembly;
        var namedBuildTargetType = editorAssembly.GetType("UnityEditor.Build.NamedBuildTarget");
        if (namedBuildTargetType != null)
        {
            var fromBuildTargetGroup = namedBuildTargetType.GetMethod(
                "FromBuildTargetGroup",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
            );
            var getAppId = typeof(PlayerSettings).GetMethod(
                "GetApplicationIdentifier",
                new[] { namedBuildTargetType }
            );

            if (fromBuildTargetGroup != null && getAppId != null)
            {
                var namedBuildTarget = fromBuildTargetGroup.Invoke(null, new object[] { group });
                if (namedBuildTarget != null)
                {
                    var appId = getAppId.Invoke(null, new[] { namedBuildTarget }) as string;
                    if (!string.IsNullOrEmpty(appId))
                    {
                        return appId;
                    }
                }
            }
        }
#endif
#pragma warning disable 618
        return PlayerSettings.GetApplicationIdentifier(group);
#pragma warning restore 618
    }

    private static (string versionNumber, string buildNumber) GetGameVersion(string platform, string channel, string appId)
    {
        var versionFromEnv = LocalSettings.Get(LocalSettings.MocaVersionNumber);
        var buildFromEnv = LocalSettings.Get(LocalSettings.MocaBuildNumber);
        var hasVersionOverride = !string.IsNullOrWhiteSpace(versionFromEnv);
        var hasBuildOverride = !string.IsNullOrWhiteSpace(buildFromEnv);
        if (hasVersionOverride && hasBuildOverride)
        {
            UnityEngine.Debug.Log("MOCABUILD: Using one-shot version override from MOCA_VERSION_NUMBER / MOCA_BUILD_NUMBER.");
            LocalSettings.ClearVersionOverrides();
            return (versionFromEnv, buildFromEnv);
        }

        if (hasVersionOverride || hasBuildOverride)
        {
            UnityEngine.Debug.LogWarning(
                "MOCABUILD: Both MOCA_VERSION_NUMBER and MOCA_BUILD_NUMBER must be set together. Ignoring and clearing one-shot overrides."
            );
            LocalSettings.ClearVersionOverrides();
        }

        var month = (channel == "apk" || channel == "xcode") ? 0 : DateTime.Now.Month;
        var defaultVersion = $"{DateTime.Now.Year % 100}.{month}.1";
        const string defaultBuild = "1";

        try
        {
            var response = FetchVersion(platform, channel, appId);
            return (response.version, response.build.ToString());
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"MOCABUILD: Fetch version failed: {ex.Message}");
            UnityEngine.Debug.LogWarning("MOCABUILD: Could not fetch version info from Titipi API service, using default project version.");
            return (defaultVersion, defaultBuild);
        }
    }

    private static VersionResponse FetchVersion(string platform, string channel, string appId)
    {
        var path = $"/v1/version/{platform}/{channel}/{appId}";
        var body = FetchJson(path);
        var parsed = JsonUtility.FromJson<VersionResponse>(body);
        if (parsed == null || string.IsNullOrEmpty(parsed.version) || parsed.build <= 0)
        {
            throw new InvalidOperationException("Failed to parse version response");
        }

        return parsed;
    }

    private static (string keystorePassword, string keyAlias, string keyPassword) GetKeystoreInfo(string appId)
    {
        var path = $"/v1/keystore/{appId}";
        var body = FetchJson(path);
        var parsed = JsonUtility.FromJson<KeystoreResponse>(body);
        if (parsed == null || string.IsNullOrEmpty(parsed.keystore_password) || string.IsNullOrEmpty(parsed.key_alias) || string.IsNullOrEmpty(parsed.key_password))
        {
            throw new InvalidOperationException("Failed to parse keystore response");
        }

        return (
            Decrypt(parsed.keystore_password, Shift),
            Decrypt(parsed.key_alias, Shift),
            Decrypt(parsed.key_password, Shift)
        );
    }

    private static string FetchJson(string path)
    {
        var localApiKey = LocalSettings.GetLocalOverride(LocalSettings.TitipiApiKey);
        var envApiKey = Environment.GetEnvironmentVariable(LocalSettings.TitipiApiKey);
        var apiKeyCandidates = new List<KeyValuePair<string, string>>();

        if (!string.IsNullOrWhiteSpace(localApiKey))
        {
            apiKeyCandidates.Add(new KeyValuePair<string, string>("local override", localApiKey.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(envApiKey))
        {
            var trimmed = envApiKey.Trim();
            var isDuplicate = false;
            foreach (var candidate in apiKeyCandidates)
            {
                if (candidate.Value == trimmed)
                {
                    isDuplicate = true;
                    break;
                }
            }

            if (!isDuplicate)
            {
                apiKeyCandidates.Add(new KeyValuePair<string, string>("environment", trimmed));
            }
        }

        if (apiKeyCandidates.Count == 0)
        {
            throw new InvalidOperationException("Missing required setting: TITIPI_API_KEY (Tools/MocaBuild/5. Settings) or environment variable.");
        }

        var url = BuildApiBaseUrl() + path;
        var lastError = string.Empty;
        foreach (var candidate in apiKeyCandidates)
        {
            using (var req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("Accept", "application/json");
                req.SetRequestHeader("User-Agent", "mocabuild-editor/1.0");
                req.SetRequestHeader("x-api-key", candidate.Value);

                var op = req.SendWebRequest();
                while (!op.isDone)
                {
                    System.Threading.Thread.Sleep(10);
                }

#if UNITY_2020_1_OR_NEWER
                var hasError = req.result != UnityWebRequest.Result.Success;
#else
                var hasError = req.isNetworkError || req.isHttpError;
#endif
                if (hasError)
                {
                    lastError = $"source={candidate.Key}, {req.responseCode}: {req.error}; body={req.downloadHandler.text}";
                    continue;
                }

                if (candidate.Key != "local override")
                {
                    UnityEngine.Debug.LogWarning("MOCABUILD: TITIPI_API_KEY local override failed. Falling back to environment key succeeded.");
                }

                return req.downloadHandler.text;
            }
        }

        throw new InvalidOperationException(
            "Failed to fetch API response: " +
            lastError +
            ". Check TITIPI_API_KEY in Tools/MocaBuild/8. Settings."
        );
    }

    private static string BuildApiBaseUrl()
    {
        var value = LocalSettings.Get(LocalSettings.TitipiApiBaseUrl);
        if (string.IsNullOrWhiteSpace(value))
        {
            value = "https://api.titipigames.com";
        }

        return value.TrimEnd('/');
    }

    private static void DeleteBuildFolder()
    {
        UnityEngine.Debug.Log("MOCABUILD: Delete `_build` folder");
        DeletePathIfExists("_build");
    }

    private static void CleanupXcodeTempFiles()
    {
        if (Application.platform != RuntimePlatform.OSXEditor)
        {
            UnityEngine.Debug.LogWarning("MOCABUILD: Skipping Xcode cleanup because this action is only available on macOS.");
            return;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        DeletePathIfExists(Path.Combine(home, "Library/Developer/Xcode/Archives"));
        DeletePathIfExists(Path.Combine(home, "Library/Developer/Xcode/DerivedData"));
    }

    private static string BinaryFileName(string productName, string versionNumber, string extension)
    {
        return $"{productName}_v{versionNumber}.{extension}";
    }

    private static string SanitizeAlphaNumeric(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
        }

        var sanitized = builder.ToString();
        return string.IsNullOrEmpty(sanitized) ? text : sanitized;
    }

    private static void PrintSummary(string productName, string platform, string appId, string versionNumber, string buildNumber)
    {
        UnityEngine.Debug.Log(
            $"MOCABUILD SUMMARY:\n" +
            $"  Product : {productName}\n" +
            $"  Platform: {platform}\n" +
            $"  App ID  : {appId}\n" +
            $"  Version : {versionNumber}\n" +
            $"  Build   : {buildNumber}"
        );
    }

    private static string Encrypt(string s, int shift)
    {
        shift %= 26;
        var chars = s.ToCharArray();

        for (var i = 0; i < chars.Length; i++)
        {
            var ch = chars[i];
            if (ch >= 'a' && ch <= 'z')
            {
                chars[i] = (char)(((ch - 'a' + shift) % 26) + 'a');
            }
            else if (ch >= 'A' && ch <= 'Z')
            {
                chars[i] = (char)(((ch - 'A' + shift) % 26) + 'A');
            }
        }

        return new string(chars);
    }

    private static string Decrypt(string s, int shift)
    {
        return Encrypt(s, 26 - (shift % 26));
    }

    private static string StringOfSeconds(int seconds)
    {
        var hours = seconds / 3600;
        var minutes = (seconds - (hours * 3600)) / 60;
        var rem = seconds % 60;

        var parts = new List<string>();
        if (hours > 0)
        {
            parts.Add(hours == 1 ? "1 hour" : $"{hours} hours");
        }

        if (minutes > 0)
        {
            parts.Add(minutes == 1 ? "1 minute" : $"{minutes} minutes");
        }

        parts.Add(rem == 1 ? "1 second" : $"{rem} seconds");
        return string.Join(", ", parts) + ".";
    }

    private static void EnsureNonEmpty(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void DeletePathIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
            return;
        }

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void RunExternalOrThrow(string fileName, string arguments, string errorMessage)
    {
        var exitCode = RunExternal(fileName, arguments, out _, out _);
        if (exitCode != 0)
        {
            throw new InvalidOperationException(errorMessage);
        }
    }

    private static int RunExternal(string fileName, string arguments, out string stdout, out string stderr)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveExecutable(fileName),
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Directory.GetCurrentDirectory()
        };

        using (var process = Process.Start(startInfo))
        {
            if (process == null)
            {
                throw new InvalidOperationException($"Failed to start process: {fileName}");
            }

            stdout = process.StandardOutput.ReadToEnd();
            stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(stdout))
            {
                UnityEngine.Debug.Log($"MOCABUILD: {fileName} {arguments}\n{stdout}");
            }

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                UnityEngine.Debug.LogWarning($"MOCABUILD: {fileName} {arguments}\n{stderr}");
            }

            return process.ExitCode;
        }
    }

    private static string ResolveExecutable(string fileName)
    {
        if (Path.IsPathRooted(fileName))
        {
            return fileName;
        }

        if (Application.platform != RuntimePlatform.WindowsEditor)
        {
            return fileName;
        }

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var pathExt = Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.BAT;.CMD;.COM";
        var extensions = pathExt.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        var hasExtension = Path.HasExtension(fileName);

        foreach (var dir in path.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir))
            {
                continue;
            }

            var basePath = Path.Combine(dir.Trim(), fileName);
            if (hasExtension && File.Exists(basePath))
            {
                return basePath;
            }

            if (!hasExtension)
            {
                foreach (var ext in extensions)
                {
                    var candidate = basePath + ext;
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }

        return fileName;
    }

}

public class MocaBuildSettingsWindow : EditorWindow
{
    private static readonly GUIStyle WrappedLabelStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };

    private string titipiApiKey;
    private string titipiApiBaseUrl;
    private string mocaVersionNumber;
    private string mocaBuildNumber;

    internal static void Open()
    {
        var window = GetWindow<MocaBuildSettingsWindow>(utility: false, title: MocaBuildEditor.SettingsTitle, focus: true);
        window.minSize = new Vector2(620f, 420f);
        window.LoadFromLocalOverrides();
    }

    private void OnEnable()
    {
        LoadFromLocalOverrides();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Persistent Local Overrides", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Values here are saved only on your local machine (user-level file). Empty fields fall back to environment variables.",
            MessageType.Info
        );
        EditorGUILayout.LabelField(
            "Shared across projects file: ~/.titipi/mocabuild-settings.json",
            EditorStyles.miniLabel
        );

        EditorGUI.BeginChangeCheck();
        titipiApiKey = DrawField("TITIPI_API_KEY", titipiApiKey, true);

        EditorGUILayout.Space(10f);
        titipiApiBaseUrl = DrawField("TITIPI_API_BASE_URL", titipiApiBaseUrl, false);

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("One-shot Build Overrides", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "MOCA_VERSION_NUMBER and MOCA_BUILD_NUMBER are session-only (not persisted), used together once, then auto-cleared.",
            MessageType.None
        );
        
        EditorGUILayout.Space(10f);
        mocaVersionNumber = DrawField("MOCA_VERSION_NUMBER", mocaVersionNumber, false);
        
        EditorGUILayout.Space(10f);
        mocaBuildNumber = DrawField("MOCA_BUILD_NUMBER", mocaBuildNumber, false);

        if (EditorGUI.EndChangeCheck())
        {
            SaveToLocalOverrides();
        }

    }

    private static string DrawField(string label, string value, bool secret)
    {
        EditorGUILayout.LabelField(label, WrappedLabelStyle);
        return secret
            ? EditorGUILayout.PasswordField(GUIContent.none, value ?? string.Empty)
            : EditorGUILayout.TextField(GUIContent.none, value ?? string.Empty);
    }

    private void LoadFromLocalOverrides()
    {
        titipiApiKey = MocaBuildEditor.LocalSettings.GetLocalOverride(MocaBuildEditor.LocalSettings.TitipiApiKey);
        titipiApiBaseUrl = MocaBuildEditor.LocalSettings.GetLocalOverride(MocaBuildEditor.LocalSettings.TitipiApiBaseUrl);
        mocaVersionNumber = MocaBuildEditor.LocalSettings.GetLocalOverride(MocaBuildEditor.LocalSettings.MocaVersionNumber);
        mocaBuildNumber = MocaBuildEditor.LocalSettings.GetLocalOverride(MocaBuildEditor.LocalSettings.MocaBuildNumber);
    }

    private void SaveToLocalOverrides()
    {
        MocaBuildEditor.LocalSettings.SetLocalOverride(MocaBuildEditor.LocalSettings.TitipiApiKey, titipiApiKey);
        MocaBuildEditor.LocalSettings.SetLocalOverride(MocaBuildEditor.LocalSettings.TitipiApiBaseUrl, titipiApiBaseUrl);
        MocaBuildEditor.LocalSettings.SetLocalOverride(MocaBuildEditor.LocalSettings.MocaVersionNumber, mocaVersionNumber);
        MocaBuildEditor.LocalSettings.SetLocalOverride(MocaBuildEditor.LocalSettings.MocaBuildNumber, mocaBuildNumber);
    }
}
