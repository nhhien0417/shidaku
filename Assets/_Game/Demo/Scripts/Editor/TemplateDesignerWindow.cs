#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Shidaku.Editor
{
    /// <summary>
    /// Interactive Template authoring tool for Shidaku - the Shidaku counterpart to smart-queens'
    /// ArtTemplate/ArtLevelGeneratorWindow (see
    /// D:\hienn\Projects\smart-queens\Assets\_Game\Scripts\Design\LevelDesign\ArtDesign), but split
    /// into its own window on purpose: this window ONLY defines shape + color (a Template asset) -
    /// paint or import a picture, nothing about Sections/Regions here at all. Once a Template looks
    /// right, open PuzzleDesignerWindow (Tools/Shidaku/Puzzle Designer) to design the Shikaku
    /// puzzle on top of it - or just click Auto-Generate there for a one-click result.
    /// </summary>
    public class TemplateDesignerWindow : EditorWindow
    {
        private enum ViewState { Setup, Editing }
        private ViewState _view = ViewState.Setup;

        private const int SandboxSize = 64; // covers the 20x20-60x60 target art size with margin
        private const float MinZoom = 0.25f;
        private const float MaxZoom = 8f;
        private const string TemplateFolder = "Assets/_Game/Demo/Templates";

        // Setup
        private Texture2D _sourceImage;
        private string _templateName = "NewTemplate";
        private bool _autoDetectGrid = true;
        private bool _removeBackgroundOnImport = true;

        // Editing
        private Template _activeAsset;
        private Template _editingTemplate;
        private Color _brushColor = Color.white;

        // Navigation
        private Vector2 _panOffset;
        private Vector2 _midBtnDownPos;
        private Rect _canvasRect;
        private float _zoom = 1f;

        [MenuItem("Tools/Shidaku/Template Designer")]
        public static void ShowWindow()
        {
            var window = GetWindow<TemplateDesignerWindow>("Template Designer");
            window.minSize = new Vector2(560, 560);
        }

        public static void OpenWithTemplate(Template template)
        {
            var window = GetWindow<TemplateDesignerWindow>("Template Designer");
            window.InitializeEditing(template);
        }

        private void OnGUI()
        {
            HandleGlobalShortcuts();
            if (_view == ViewState.Setup) DrawSetupView();
            else DrawEditorView();
        }

        // ---------------------------------------------------------------- setup

        private void InitializeEditing(Template asset)
        {
            _activeAsset = asset;
            if (asset != null) _templateName = asset.name;

            _editingTemplate = CreateInstance<Template>();
            _editingTemplate.Initialize(SandboxSize, SandboxSize);
            _editingTemplate.hideFlags = HideFlags.HideAndDontSave;

            if (asset != null)
            {
                int offsetX = (SandboxSize - asset.Width) / 2;
                int offsetY = (SandboxSize - asset.Height) / 2;
                for (int y = 0; y < asset.Height; y++)
                    for (int x = 0; x < asset.Width; x++)
                        _editingTemplate.SetPixel(offsetX + x, offsetY + y, asset.GetColor(x, y));
            }

            _view = ViewState.Editing;
            ResetNavigation();
        }

        private void DrawSetupView()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Create New Template", EditorStyles.boldLabel);
            _templateName = EditorGUILayout.TextField("Name", _templateName);
            if (GUILayout.Button("New Template", GUILayout.Height(30)))
                InitializeEditing(null);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Import From Image", EditorStyles.boldLabel);
            _sourceImage = (Texture2D)EditorGUILayout.ObjectField("Image", _sourceImage, typeof(Texture2D), false);
            _autoDetectGrid = EditorGUILayout.Toggle(new GUIContent("Auto-detect grid", "ON for reference-chart-style pictures (real resolution much bigger than the logical cell grid, thin grid lines drawn in, e.g. a cross-stitch/perler-bead pattern chart). OFF for pictures that are already 1 image pixel = 1 game cell (like Assets/_Game/Demo/SourceArt)."), _autoDetectGrid);
            if (_autoDetectGrid)
                _removeBackgroundOnImport = EditorGUILayout.Toggle(new GUIContent("Remove background", "Flood-fills away the picture's border/background color so it becomes transparent (not part of the artwork)."), _removeBackgroundOnImport);
            if (GUILayout.Button("Import & Edit", GUILayout.Height(30)))
            {
                if (PerformImageImport()) { _view = ViewState.Editing; ResetNavigation(); }
                else EditorUtility.DisplayDialog("Import Failed", "Could not import this image - see Console for the specific reason (no grid detected, or the picture is fully transparent/empty). Try toggling Auto-detect grid.", "OK");
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Edit Existing Template", EditorStyles.boldLabel);
            _activeAsset = (Template)EditorGUILayout.ObjectField("Template", _activeAsset, typeof(Template), false);
            if (_activeAsset != null && GUILayout.Button("Resume Editing", GUILayout.Height(30)))
                InitializeEditing(_activeAsset);
            EditorGUILayout.EndVertical();
        }

        private bool PerformImageImport()
        {
            if (_sourceImage == null) return false;
            string path = AssetDatabase.GetAssetPath(_sourceImage);
            if (string.IsNullOrEmpty(path)) return false;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            bool originalReadable = importer != null && importer.isReadable;
            var originalCompression = importer != null ? importer.textureCompression : TextureImporterCompression.Uncompressed;
            Texture2D readable = _sourceImage;

            try
            {
                if (importer != null && (!importer.isReadable || importer.textureCompression != TextureImporterCompression.Uncompressed))
                {
                    importer.isReadable = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                    readable = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }

                Color32[] pixels;
                int srcW, srcH;

                if (_autoDetectGrid)
                {
                    if (!ArtImageProcessor.TryDetectAndDownscale(readable, out pixels, out srcW, out srcH, _removeBackgroundOnImport))
                        return false; // reason already logged to Console by ArtImageProcessor
                }
                else
                {
                    // Already pixel-perfect (1 image pixel = 1 game cell) - copy as-is, no downscale.
                    srcW = readable.width;
                    srcH = readable.height;
                    pixels = readable.GetPixels32();
                }

                int w = Mathf.Min(srcW, SandboxSize);
                int h = Mathf.Min(srcH, SandboxSize);
                if (w <= 0 || h <= 0) return false;
                if (w < srcW || h < srcH)
                    Debug.LogWarning($"TemplateDesignerWindow: '{readable.name}' downscaled to {srcW}x{srcH} but the sandbox only holds {SandboxSize}x{SandboxSize} - cropped to {w}x{h}. Increase SandboxSize if this keeps happening.");

                _editingTemplate = CreateInstance<Template>();
                _editingTemplate.Initialize(SandboxSize, SandboxSize);
                _editingTemplate.hideFlags = HideFlags.HideAndDontSave;

                int offsetX = (SandboxSize - w) / 2;
                int offsetY = (SandboxSize - h) / 2;
                bool found = false;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        Color32 c = pixels[y * srcW + x];
                        if (c.a != 0)
                        {
                            _editingTemplate.SetPixel(offsetX + x, offsetY + y, c);
                            found = true;
                        }
                    }
                }

                if (string.IsNullOrEmpty(_templateName) || _templateName == "NewTemplate")
                    _templateName = readable.name;

                return found;
            }
            finally
            {
                if (importer != null)
                {
                    importer.isReadable = originalReadable;
                    importer.textureCompression = originalCompression;
                    importer.SaveAndReimport();
                }
            }
        }

        // ---------------------------------------------------------------- editor view

        private void DrawEditorView()
        {
            if (_editingTemplate == null) { _view = ViewState.Setup; return; }

            DrawTopToolbar();

            _canvasRect = new Rect(0, 40, position.width, position.height - 80);
            HandleNavigationEvents(_canvasRect);

            GUI.BeginGroup(_canvasRect);
            DrawCanvasContent(_canvasRect);
            HandlePaintingEvents(_canvasRect);
            GUI.EndGroup();

            DrawBottomBar();

            if (GUI.changed) Repaint();
        }

        private void DrawTopToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Exit", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("Unsaved Changes", "Save template before exiting?", "Save", "Discard"))
                    SaveTemplateAsset();
                _view = ViewState.Setup;
            }

            _brushColor = EditorGUILayout.ColorField(GUIContent.none, _brushColor, false, false, false, GUILayout.Width(50));
            GUILayout.FlexibleSpace();
            _templateName = GUILayout.TextField(_templateName, GUILayout.Width(150));
            if (GUILayout.Button("Save Template", EditorStyles.toolbarButton))
                SaveTemplateAsset();
            GUILayout.EndHorizontal();
        }

        private void DrawBottomBar()
        {
            Rect barRect = new(0, position.height - 40, position.width, 40);
            GUILayout.BeginArea(barRect);
            GUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.35f, 0.65f, 1f);
            if (GUILayout.Button("SAVE && DESIGN PUZZLE >", GUILayout.Height(30)))
                EditorApplication.delayCall += SaveAndOpenPuzzleDesigner;
            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawCanvasContent(Rect rect)
        {
            float cellSize = 15f * _zoom;
            float totalWidth = SandboxSize * cellSize;
            float totalHeight = SandboxSize * cellSize;
            float startX = rect.width / 2 + _panOffset.x - totalWidth / 2;
            float startY = rect.height / 2 + _panOffset.y - totalHeight / 2;

            for (int y = 0; y < SandboxSize; y++)
            {
                for (int x = 0; x < SandboxSize; x++)
                {
                    Rect cellRect = new(startX + x * cellSize, startY + (SandboxSize - 1 - y) * cellSize, cellSize, cellSize);
                    if (cellRect.xMax < 0 || cellRect.x > rect.width || cellRect.yMax < 0 || cellRect.y > rect.height) continue;

                    bool playable = _editingTemplate.IsPlayable(x, y);
                    Color drawColor = playable
                        ? (Color)_editingTemplate.GetColor(x, y)
                        : ((x + y) % 2 == 0 ? new Color(0.45f, 0.45f, 0.45f) : new Color(0.55f, 0.55f, 0.55f));
                    EditorGUI.DrawRect(cellRect, drawColor);
                }
            }
        }

        // ---------------------------------------------------------------- input

        private void HandleNavigationEvents(Rect rect)
        {
            Event e = Event.current;
            if (!rect.Contains(e.mousePosition)) return;

            if (e.type == EventType.ScrollWheel)
            {
                _zoom = Mathf.Clamp(_zoom - e.delta.y * 0.05f, MinZoom, MaxZoom);
                e.Use();
            }

            if (e.button == 2)
            {
                if (e.type == EventType.MouseDown && !e.alt) _midBtnDownPos = e.mousePosition;
                else if (e.type == EventType.MouseUp && !e.alt)
                {
                    if (Vector2.Distance(_midBtnDownPos, e.mousePosition) < 3f) ShowNativeColorPicker();
                }
                else if (e.type == EventType.MouseDrag)
                {
                    _panOffset += e.delta;
                    GUI.changed = true;
                    e.Use();
                }
            }

            if (e.alt && e.button == 0 && (e.type == EventType.MouseDrag || e.type == EventType.MouseDown))
            {
                _panOffset += e.delta;
                GUI.changed = true;
                e.Use();
            }
        }

        private void HandlePaintingEvents(Rect rect)
        {
            Event e = Event.current;
            if (!rect.Contains(e.mousePosition) || e.alt) return;

            if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
            {
                float cellSize = 15f * _zoom;
                float startX = rect.width / 2 + _panOffset.x - SandboxSize * cellSize / 2;
                float startY = rect.height / 2 + _panOffset.y - SandboxSize * cellSize / 2;

                int gx = Mathf.FloorToInt((e.mousePosition.x - startX) / cellSize);
                int gyScreen = Mathf.FloorToInt((e.mousePosition.y - startY) / cellSize);
                int gy = SandboxSize - 1 - gyScreen;

                if (gx >= 0 && gx < SandboxSize && gy >= 0 && gy < SandboxSize)
                {
                    if (e.button == 0)
                    {
                        if (!_editingTemplate.IsPlayable(gx, gy) || (Color)_editingTemplate.GetColor(gx, gy) != _brushColor)
                        {
                            Undo.RegisterCompleteObjectUndo(_editingTemplate, "Paint Art");
                            _editingTemplate.SetPixel(gx, gy, _brushColor);
                            GUI.changed = true;
                        }
                    }
                    else if (e.button == 1)
                    {
                        if (_editingTemplate.IsPlayable(gx, gy))
                        {
                            Undo.RegisterCompleteObjectUndo(_editingTemplate, "Erase Art");
                            _editingTemplate.Erase(gx, gy);
                            GUI.changed = true;
                        }
                    }
                }
                if (e.type == EventType.MouseDown) e.Use();
            }
        }

        private void HandleGlobalShortcuts()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.control)
            {
                if (e.keyCode == KeyCode.Z)
                {
                    if (e.shift) Undo.PerformRedo(); else Undo.PerformUndo();
                    GUI.changed = true;
                    e.Use();
                }
            }
        }

        private void ResetNavigation()
        {
            _panOffset = Vector2.zero;
            _zoom = 1f;
        }

        private void ShowNativeColorPicker()
        {
            System.Type colorPickerType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ColorPicker");
            if (colorPickerType == null) return;

            System.Action<Color> callback = newColor => { _brushColor = newColor; Repaint(); };
            MethodInfo bestMethod = null;
            foreach (var m in colorPickerType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (m.Name != "Show") continue;
                var p = m.GetParameters();
                if (p.Length >= 2 && p[0].ParameterType.Name.Contains("Action") && p[1].ParameterType == typeof(Color))
                {
                    bestMethod = m;
                    break;
                }
            }
            if (bestMethod == null) return;

            var ps = bestMethod.GetParameters();
            object[] args = new object[ps.Length];
            args[0] = callback;
            args[1] = _brushColor;
            for (int i = 2; i < ps.Length; i++)
                args[i] = ps[i].ParameterType == typeof(bool) ? false : ps[i].ParameterType == typeof(string) ? "Brush Color" : null;
            bestMethod.Invoke(null, args);
        }

        // ---------------------------------------------------------------- save

        private void SaveTemplateAsset()
        {
            if (_editingTemplate == null) return;

            int minX = SandboxSize, minY = SandboxSize, maxX = -1, maxY = -1;
            bool hasContent = false;
            for (int y = 0; y < SandboxSize; y++)
            {
                for (int x = 0; x < SandboxSize; x++)
                {
                    if (_editingTemplate.IsPlayable(x, y))
                    {
                        minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                        hasContent = true;
                    }
                }
            }

            if (!hasContent)
            {
                EditorUtility.DisplayDialog("Error", "Canvas is empty. Nothing to save.", "OK");
                return;
            }

            int w = maxX - minX + 1, h = maxY - minY + 1;

            if (_activeAsset == null)
            {
                if (!Directory.Exists(TemplateFolder)) Directory.CreateDirectory(TemplateFolder);
                string uniquePath = AssetDatabase.GenerateUniqueAssetPath($"{TemplateFolder}/{_templateName}.asset");
                _activeAsset = CreateInstance<Template>();
                AssetDatabase.CreateAsset(_activeAsset, uniquePath);
                _templateName = Path.GetFileNameWithoutExtension(uniquePath);
            }
            else if (_activeAsset.name != _templateName && !string.IsNullOrEmpty(_templateName))
            {
                string oldPath = AssetDatabase.GetAssetPath(_activeAsset);
                string uniqueNewPath = AssetDatabase.GenerateUniqueAssetPath($"{TemplateFolder}/{_templateName}.asset");
                string uniqueNewName = Path.GetFileNameWithoutExtension(uniqueNewPath);
                if (string.IsNullOrEmpty(AssetDatabase.RenameAsset(oldPath, uniqueNewName)))
                {
                    _templateName = uniqueNewName;
                    AssetDatabase.SaveAssets();
                }
            }

            _activeAsset.Initialize(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    _activeAsset.SetPixel(x, y, _editingTemplate.GetColor(minX + x, minY + y));

            EditorUtility.SetDirty(_activeAsset);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Template Designer] Template saved: {AssetDatabase.GetAssetPath(_activeAsset)}");
        }

        private void SaveAndOpenPuzzleDesigner()
        {
            SaveTemplateAsset();
            if (_activeAsset == null) return;
            PuzzleDesignerWindow.OpenForTemplate(_activeAsset);
        }
    }
}
#endif
