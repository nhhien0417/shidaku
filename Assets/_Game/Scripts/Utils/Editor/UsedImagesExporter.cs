using UnityEditor;
using UnityEngine;
#if ADDRESSABLES
using UnityEditor.AddressableAssets;
#endif
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class UsedImagesExporter : EditorWindow
{
    private static string _exportFolder => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "UsedImagesExport");

    private bool exportImages = true;
    private bool exportAudios = true;
    private bool exportFonts = true;
    private bool maintainFolderStructure = true;

    [MenuItem("Tools/Export Used Images")]
    public static void ShowWindow()
    {
        GetWindow<UsedImagesExporter>("Used Images Exporter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Asset Types to Export", EditorStyles.boldLabel);
        exportImages = EditorGUILayout.Toggle("Images (.png, .jpg, .psd...)", exportImages);
        exportAudios = EditorGUILayout.Toggle("Audios (.mp3, .wav...)", exportAudios);
        exportFonts = EditorGUILayout.Toggle("Fonts (.ttf, .otf)", exportFonts);

        GUILayout.Space(10);
        
        GUILayout.Label("Export Options", EditorStyles.boldLabel);
        maintainFolderStructure = EditorGUILayout.Toggle("Keep Folder Structure", maintainFolderStructure);

        GUILayout.Space(20);

        if (GUILayout.Button("Scan and Export Assets", GUILayout.Height(40)))
        {
            ScanAndExport();
        }

        if (Directory.Exists(_exportFolder))
        {
            GUILayout.Space(10);
            GUILayout.Label($"Export Folder: {_exportFolder}", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Open Export Folder"))
            {
                EditorUtility.RevealInFinder(_exportFolder);
            }
        }
    }

    private void ScanAndExport()
    {
        List<Object> roots = new List<Object>();

        try
        {
            // 1. GATHER ROOTS
            EditorUtility.DisplayProgressBar("Scanning", "Collecting Scenes...", 0.1f);
            foreach (var scene in EditorBuildSettings.scenes.Where(s => s.enabled))
            {
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
                if (sceneAsset != null) roots.Add(sceneAsset);
            }

            EditorUtility.DisplayProgressBar("Scanning", "Collecting Resources...", 0.2f);
            var resourcePaths = AssetDatabase.FindAssets("", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).Where(p => p.Contains("/Resources/"));
            foreach (var path in resourcePaths)
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (obj != null) roots.Add(obj);
            }

#if ADDRESSABLES
            EditorUtility.DisplayProgressBar("Scanning", "Collecting Addressables...", 0.3f);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
            {
                foreach (var group in settings.groups)
                {
                    foreach (var entry in group.entries)
                    {
                        var obj = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(entry.guid));
                        if (obj != null) roots.Add(obj);
                    }
                }
            }
#endif

            // 2. DEEP SCAN DEPENDENCIES
            EditorUtility.DisplayProgressBar("Scanning", "Resolving Deep Dependencies...", 0.5f);
            Object[] dependencies = EditorUtility.CollectDependencies(roots.ToArray());

            // 3. FILTER BY EXTENSIONS
            HashSet<string> validExtensions = GetValidExtensions();
            HashSet<string> assetsToExport = new HashSet<string>();

            foreach (var dep in dependencies)
            {
                if (dep == null) continue;
                string path = AssetDatabase.GetAssetPath(dep);
                
                if (string.IsNullOrEmpty(path) || !File.Exists(path) || path.EndsWith(".cs")) continue;

                string ext = Path.GetExtension(path).ToLower();
                if (validExtensions.Contains(ext))
                {
                    assetsToExport.Add(path);
                }
            }

            // 4. EXPORT FILES
            ExportFiles(assetsToExport);
            
            Debug.Log($"<color=green>Export thành công! Đã trích xuất {assetsToExport.Count} assets cho Creative.</color>");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private HashSet<string> GetValidExtensions()
    {
        HashSet<string> exts = new HashSet<string>();
        if (exportImages) exts.UnionWith(new[] { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tiff", ".gif", ".webp" });
        if (exportAudios) exts.UnionWith(new[] { ".mp3", ".wav", ".ogg", ".aif", ".aiff" });
        if (exportFonts) exts.UnionWith(new[] { ".ttf", ".otf" });
        return exts;
    }

    private void ExportFiles(HashSet<string> paths)
    {
        if (Directory.Exists(_exportFolder))
        {
            Directory.Delete(_exportFolder, true);
        }
        Directory.CreateDirectory(_exportFolder);

        int count = 0;
        int total = paths.Count;
        HashSet<string> flatUsedNames = new HashSet<string>();

        foreach (var srcPath in paths)
        {
            count++;
            EditorUtility.DisplayProgressBar("Exporting", $"Copying {count}/{total} files...", 0.6f + (0.4f * ((float)count / total)));

            string dstPath;

            if (maintainFolderStructure)
            {
                dstPath = Path.Combine(_exportFolder, srcPath);
                string dstDir = Path.GetDirectoryName(dstPath);
                if (!Directory.Exists(dstDir)) Directory.CreateDirectory(dstDir);
            }
            else
            {
                // Flat folder fallback (nếu user tắt Maintain Folder Structure)
                string fileName = Path.GetFileName(srcPath);
                string cleanName = SanitizeFileName(fileName, flatUsedNames);
                dstPath = Path.Combine(_exportFolder, cleanName);
            }

            File.Copy(srcPath, dstPath, overwrite: true);
        }
    }

    private string SanitizeFileName(string originalName, HashSet<string> usedNames)
    {
        string name = Path.GetFileNameWithoutExtension(originalName);
        string ext = Path.GetExtension(originalName);
        string result = name + ext;
        int counter = 1;

        while (usedNames.Contains(result))
        {
            result = $"{name}_{counter++}{ext}";
        }

        usedNames.Add(result);
        return result;
    }
}
