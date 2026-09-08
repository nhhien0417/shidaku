#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Shidaku.Editor
{
    /// <summary>
    /// Puzzle designer - the second half of the Shidaku authoring pipeline, on top of an already-
    /// painted Template. A PuzzleData asset IS the final playable puzzle - there is no separate
    /// "bake to a different asset/folder" step; Verify just checks the same asset that's already
    /// saved in Assets/_Game/Demo/Puzzles/, and Save rebuilds the runtime PuzzleCatalog to include
    /// it. Two ways to design, freely mixable:
    ///   Fast path  - "Auto-Generate" does everything in one click (auto-cluster Sections, auto-
    ///                fill every Region, verify, save) - no manual editing needed at all.
    ///   Manual path - Sections mode assigns cells to Sections (click a section swatch, then
    ///                 drag to paint; Auto-Cluster gives a starting point to hand-adjust), Regions
    ///                 mode drags out rectangle Regions directly on the art (same shape the player
    ///                 would draw in-game), with an Auto-Fill-Remaining shortcut per section.
    /// Verify mode re-derives the exact hard invariants this project has been bitten by before
    /// (region monochrome + fully covers its cells, every playable picture cell claimed by exactly
    /// one Section).
    /// </summary>
    public class PuzzleDesignerWindow : EditorWindow
    {
        private enum ViewState { Setup, Editing }
        private enum Mode { Sections, Regions, Verify }

        private const float MinZoom = 0.25f;
        private const float MaxZoom = 8f;
        private const string PuzzleFolder = "Assets/_Game/Demo/Puzzles";
        private const string CatalogDir = "Assets/_Game/Demo/Resources";
        private const string CatalogAssetName = "PuzzleCatalog";
        private const int MaxRegionArea = 15;
        private const int TargetCellsPerSection = 30; // same heuristic the old batch pipeline used
        private const int MinSections = 3;
        private const int MaxSections = 6;

        private ViewState _view = ViewState.Setup;
        private Mode _mode = Mode.Sections;

        // Setup
        private Template _newFromTemplate;
        private PuzzleData _activeAsset;
        private string _puzzleName = "NewPuzzle";

        // Editing (the PuzzleData asset is edited in place via Undo - it's already the final
        // playable asset, no separate sandbox copy needed).
        private PuzzleData _puzzle;

        // Navigation
        private Vector2 _panOffset;
        private float _zoom = 3f;
        private Rect _canvasRect;

        // Sections mode
        private int _currentSectionIndex;

        // Regions mode
        private int _editingSectionIndex;
        private bool _dragging;
        private Vector2Int _dragStart;
        private Vector2Int _dragEnd;

        // Verify
        private string _verifyReport = "";
        private bool _verifyOk;

        [MenuItem("Tools/Shidaku/Puzzle Designer")]
        public static void ShowWindow()
        {
            var window = GetWindow<PuzzleDesignerWindow>("Puzzle Designer");
            window.minSize = new Vector2(640, 620);
        }

        public static void OpenWithPuzzleData(PuzzleData puzzle)
        {
            var window = GetWindow<PuzzleDesignerWindow>("Puzzle Designer");
            window.InitializeEditing(puzzle);
        }

        /// <summary>Entry point from TemplateDesignerWindow's "Save && Design Puzzle" button.</summary>
        public static void OpenForTemplate(Template template)
        {
            var window = GetWindow<PuzzleDesignerWindow>("Puzzle Designer");
            window._newFromTemplate = template;
            window._view = ViewState.Setup;
            window.Repaint();
        }

        private void OnGUI()
        {
            if (_view == ViewState.Setup) DrawSetupView();
            else DrawEditingView();
        }

        // ---------------------------------------------------------------- setup

        private void InitializeEditing(PuzzleData puzzle)
        {
            _activeAsset = puzzle;
            _puzzle = puzzle;
            _puzzle.SyncSizeFromSourceArt();
            _mode = Mode.Sections;
            _currentSectionIndex = Mathf.Max(0, _puzzle.Sections.Count - 1);
            _editingSectionIndex = 0;
            _verifyReport = "";
            _view = ViewState.Editing;
            ResetNavigation();
        }

        private void DrawSetupView()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("New Puzzle From Template", EditorStyles.boldLabel);
            _newFromTemplate = (Template)EditorGUILayout.ObjectField("Template", _newFromTemplate, typeof(Template), false);
            _puzzleName = EditorGUILayout.TextField("Name", _puzzleName);

            GUILayout.Space(4);
            using (new EditorGUI.DisabledScope(_newFromTemplate == null))
            {
                GUI.backgroundColor = new Color(0.3f, 0.75f, 0.35f);
                if (GUILayout.Button("⚡ Auto-Generate Puzzle && Save", GUILayout.Height(34)))
                    EditorApplication.delayCall += AutoGenerateFromSetup;
                GUI.backgroundColor = Color.white;
                GUILayout.Label("One click: clusters Sections, fills every Region, verifies, and saves. No manual editing needed.", EditorStyles.miniLabel);

                GUILayout.Space(4);
                if (GUILayout.Button("Create && Edit Manually", GUILayout.Height(26)))
                    CreateNewPuzzle();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Edit Existing Puzzle", EditorStyles.boldLabel);
            _activeAsset = (PuzzleData)EditorGUILayout.ObjectField("Puzzle", _activeAsset, typeof(PuzzleData), false);
            if (_activeAsset != null && GUILayout.Button("Resume Editing", GUILayout.Height(26)))
                InitializeEditing(_activeAsset);
            EditorGUILayout.EndVertical();
        }

        private PuzzleData CreateNewPuzzle()
        {
            if (_newFromTemplate == null) return null;
            if (!Directory.Exists(PuzzleFolder)) Directory.CreateDirectory(PuzzleFolder);

            string name = string.IsNullOrEmpty(_puzzleName) ? _newFromTemplate.name : _puzzleName;
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath($"{PuzzleFolder}/{name}.asset");

            var puzzle = CreateInstance<PuzzleData>();
            puzzle.SourceArt = _newFromTemplate;
            puzzle.SyncSizeFromSourceArt();
            AssetDatabase.CreateAsset(puzzle, uniquePath);
            AssetDatabase.SaveAssets();

            InitializeEditing(puzzle);
            return puzzle;
        }

        /// <summary>The one-click fast path: create the PuzzleData, auto-cluster, auto-fill every
        /// section, verify, and save - all in one go. Falls back to opening the puzzle in Verify
        /// mode (instead of silently failing) if anything doesn't check out, so there's always a
        /// way to see what went wrong and finish by hand.</summary>
        private void AutoGenerateFromSetup()
        {
            var puzzle = CreateNewPuzzle();
            if (puzzle == null) return;

            AutoGenerateEverything();
            RunVerify();

            if (_verifyOk)
            {
                SaveAndRegister();
            }
            else
            {
                _mode = Mode.Verify;
                EditorUtility.DisplayDialog("Auto-Generate", "Auto-generate finished but didn't pass verification - opening Verify tab so you can review/finish by hand.", "OK");
            }
        }

        // ---------------------------------------------------------------- editing shell

        private void DrawEditingView()
        {
            if (_puzzle == null || _puzzle.SourceArt == null) { _view = ViewState.Setup; return; }

            DrawTopToolbar();

            if (_mode == Mode.Verify)
            {
                DrawVerifyView();
            }
            else
            {
                DrawModeSubToolbar();
                _canvasRect = new Rect(0, 90, position.width, position.height - 90);
                HandleNavigationEvents(_canvasRect);

                GUI.BeginGroup(_canvasRect);
                DrawCanvas(_canvasRect);
                if (_mode == Mode.Sections) HandleSectionPaintEvents(_canvasRect);
                else HandleRegionDrawEvents(_canvasRect);
                GUI.EndGroup();
            }

            if (GUI.changed) Repaint();
        }

        private void DrawTopToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Exit", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                EditorUtility.SetDirty(_puzzle);
                AssetDatabase.SaveAssets();
                _view = ViewState.Setup;
            }

            GUILayout.Space(10);
            _mode = (Mode)GUILayout.Toolbar((int)_mode, new[] { "Sections", "Regions", "Verify" }, EditorStyles.toolbarButton, GUILayout.Width(220));
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.3f, 0.75f, 0.35f);
            if (GUILayout.Button("⚡ Auto-Generate Everything", EditorStyles.toolbarButton, GUILayout.Width(190)))
            {
                if (EditorUtility.DisplayDialog("Auto-Generate", "Replace ALL current Sections/Regions with a fresh auto-generated layout?", "Auto-Generate", "Cancel"))
                    AutoGenerateEverything();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                EditorUtility.SetDirty(_puzzle);
                AssetDatabase.SaveAssets();
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(AssetDatabase.GetAssetPath(_puzzle), EditorStyles.miniLabel);
        }

        private void DrawModeSubToolbar()
        {
            Rect barRect = new(0, 40, position.width, 50);
            GUILayout.BeginArea(barRect, EditorStyles.helpBox);

            if (_mode == Mode.Sections)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Painting:", GUILayout.Width(56));
                DrawSectionSwatchRow(ref _currentSectionIndex);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear All", GUILayout.Width(70)))
                {
                    if (EditorUtility.DisplayDialog("Clear All", "Clear every section assignment and all regions?", "Clear", "Cancel"))
                    {
                        Undo.RegisterCompleteObjectUndo(_puzzle, "Clear Sections");
                        for (int i = 0; i < _puzzle.SectionOf.Length; i++) _puzzle.SectionOf[i] = -1;
                        _puzzle.Sections.Clear();
                        _currentSectionIndex = 0;
                        GUI.changed = true;
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Label("Left-drag: paint the selected section onto cells.  Right-drag: clear assignment.  Click + to add a section.", EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Section:", GUILayout.Width(56));
                DrawSectionSwatchRow(ref _editingSectionIndex, allowAddNew: false);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Auto-Fill Remaining", GUILayout.Width(140)))
                    AutoFillSection(_editingSectionIndex);
                if (GUILayout.Button("Clear Regions", GUILayout.Width(90)))
                {
                    if (_editingSectionIndex < _puzzle.Sections.Count)
                    {
                        Undo.RegisterCompleteObjectUndo(_puzzle, "Clear Regions");
                        _puzzle.Sections[_editingSectionIndex].Regions.Clear();
                        GUI.changed = true;
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Label("Left-drag on unfilled cells: draw a new region (clue = drag start cell).  Right-click a region: delete it.", EditorStyles.miniLabel);
            }

            GUILayout.EndArea();
        }

        /// <summary>Row of clickable colored swatches, one per existing Section - replaces typing a
        /// section index by hand. The selected swatch gets bold white text. Optionally ends with a
        /// "+" button to append (and immediately select) a new section.</summary>
        private void DrawSectionSwatchRow(ref int selected, bool allowAddNew = true)
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < _puzzle.Sections.Count; i++)
            {
                var prevColor = GUI.backgroundColor;
                GUI.backgroundColor = PuzzleColors.SectionTint(i);
                var style = i == selected ? SelectedSwatchStyle() : GUI.skin.button;
                if (GUILayout.Button((i + 1).ToString(), style, GUILayout.Width(28), GUILayout.Height(24)))
                    selected = i;
                GUI.backgroundColor = prevColor;
            }
            if (allowAddNew && GUILayout.Button("+", GUILayout.Width(28), GUILayout.Height(24)))
                selected = _puzzle.EnsureSectionCount(_puzzle.Sections.Count + 1) - 1;
            GUILayout.EndHorizontal();
        }

        private static GUIStyle _selectedSwatchStyle;
        private static GUIStyle SelectedSwatchStyle()
        {
            if (_selectedSwatchStyle == null)
            {
                _selectedSwatchStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
                _selectedSwatchStyle.normal.textColor = Color.white;
                _selectedSwatchStyle.active.textColor = Color.white;
            }
            return _selectedSwatchStyle;
        }

        // ---------------------------------------------------------------- canvas / navigation

        private (float cellSize, float startX, float startY) CanvasMetrics(Rect rect)
        {
            float cellSize = 15f * _zoom;
            float totalWidth = _puzzle.Width * cellSize;
            float totalHeight = _puzzle.Height * cellSize;
            float startX = rect.width / 2 + _panOffset.x - totalWidth / 2;
            float startY = rect.height / 2 + _panOffset.y - totalHeight / 2;
            return (cellSize, startX, startY);
        }

        private void HandleNavigationEvents(Rect rect)
        {
            Event e = Event.current;
            if (!rect.Contains(e.mousePosition)) return;

            if (e.type == EventType.ScrollWheel)
            {
                _zoom = Mathf.Clamp(_zoom - e.delta.y * 0.05f, MinZoom, MaxZoom);
                e.Use();
            }

            if ((e.button == 2 || (e.alt && e.button == 0)) && (e.type == EventType.MouseDrag || e.type == EventType.MouseDown))
            {
                if (e.type == EventType.MouseDrag)
                {
                    _panOffset += e.delta;
                    GUI.changed = true;
                    e.Use();
                }
            }
        }

        private bool TryGetCellUnderMouse(Rect rect, Vector2 mousePos, out int gx, out int gy)
        {
            var (cellSize, startX, startY) = CanvasMetrics(rect);
            gx = Mathf.FloorToInt((mousePos.x - startX) / cellSize);
            int gyScreen = Mathf.FloorToInt((mousePos.y - startY) / cellSize);
            gy = _puzzle.Height - 1 - gyScreen;
            return gx >= 0 && gx < _puzzle.Width && gy >= 0 && gy < _puzzle.Height;
        }

        private void DrawCanvas(Rect rect)
        {
            var (cellSize, startX, startY) = CanvasMetrics(rect);

            bool[] coveredByRegion = null;
            if (_mode == Mode.Regions && _editingSectionIndex < _puzzle.Sections.Count)
            {
                coveredByRegion = new bool[_puzzle.Width * _puzzle.Height];
                foreach (var region in _puzzle.Sections[_editingSectionIndex].Regions)
                    for (int y = region.Rect.y; y < region.Rect.y + region.Rect.height; y++)
                        for (int x = region.Rect.x; x < region.Rect.x + region.Rect.width; x++)
                            coveredByRegion[y * _puzzle.Width + x] = true;
            }

            for (int y = 0; y < _puzzle.Height; y++)
            {
                for (int x = 0; x < _puzzle.Width; x++)
                {
                    Rect cellRect = new(startX + x * cellSize, startY + (_puzzle.Height - 1 - y) * cellSize, cellSize, cellSize);
                    if (cellRect.xMax < 0 || cellRect.x > rect.width || cellRect.yMax < 0 || cellRect.y > rect.height) continue;

                    if (!_puzzle.IsPlayable(x, y))
                    {
                        Color emptyColor = (x + y) % 2 == 0 ? new Color(0.45f, 0.45f, 0.45f) : new Color(0.55f, 0.55f, 0.55f);
                        EditorGUI.DrawRect(cellRect, emptyColor);
                        continue;
                    }

                    Color art = _puzzle.SourceArt.GetColor(x, y);
                    int section = _puzzle.GetSectionAt(x, y);

                    if (_mode == Mode.Sections)
                    {
                        Color tint = PuzzleColors.SectionTint(section);
                        Color drawColor = section < 0 ? tint : Color.Lerp(art, tint, 0.55f);
                        EditorGUI.DrawRect(cellRect, drawColor);
                    }
                    else
                    {
                        bool inEditingSection = section == _editingSectionIndex;
                        if (!inEditingSection)
                        {
                            EditorGUI.DrawRect(cellRect, Color.Lerp(art, Color.black, 0.75f));
                        }
                        else
                        {
                            bool covered = coveredByRegion != null && coveredByRegion[y * _puzzle.Width + x];
                            Color drawColor = covered ? art : Color.Lerp(art, Color.yellow, 0.35f);
                            EditorGUI.DrawRect(cellRect, drawColor);
                        }
                    }
                }
            }

            if (_mode == Mode.Regions)
            {
                DrawRegionOutlines(rect, cellSize, startX, startY);
                if (_dragging) DrawDragPreview(rect, cellSize, startX, startY);
            }
        }

        private void DrawRegionOutlines(Rect rect, float cellSize, float startX, float startY)
        {
            if (_editingSectionIndex >= _puzzle.Sections.Count) return;
            var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.black;

            foreach (var region in _puzzle.Sections[_editingSectionIndex].Regions)
            {
                Rect r = new(
                    startX + region.Rect.x * cellSize,
                    startY + (_puzzle.Height - region.Rect.y - region.Rect.height) * cellSize,
                    region.Rect.width * cellSize,
                    region.Rect.height * cellSize);

                Handles.BeginGUI();
                Handles.color = Color.black;
                Handles.DrawSolidRectangleWithOutline(r, Color.clear, Color.black);
                Handles.EndGUI();

                if (cellSize >= 8f)
                    GUI.Label(r, (region.Rect.width * region.Rect.height).ToString(), style);
            }
        }

        private void DrawDragPreview(Rect rect, float cellSize, float startX, float startY)
        {
            var r = MakeRect(_dragStart, _dragEnd);
            Rect screenRect = new(
                startX + r.x * cellSize,
                startY + (_puzzle.Height - r.y - r.height) * cellSize,
                r.width * cellSize,
                r.height * cellSize);
            EditorGUI.DrawRect(screenRect, new Color(1f, 1f, 1f, 0.25f));
            Handles.BeginGUI();
            Handles.DrawSolidRectangleWithOutline(screenRect, Color.clear, Color.white);
            Handles.EndGUI();
        }

        private static RectInt MakeRect(Vector2Int a, Vector2Int b)
        {
            int minX = Mathf.Min(a.x, b.x), minY = Mathf.Min(a.y, b.y);
            int maxX = Mathf.Max(a.x, b.x), maxY = Mathf.Max(a.y, b.y);
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private void ResetNavigation()
        {
            _panOffset = Vector2.zero;
            _zoom = 3f;
        }

        // ---------------------------------------------------------------- sections mode input

        private void HandleSectionPaintEvents(Rect rect)
        {
            Event e = Event.current;
            if (!rect.Contains(e.mousePosition) || e.alt) return;
            if (e.type != EventType.MouseDown && e.type != EventType.MouseDrag) return;
            if (e.button != 0 && e.button != 1) return;
            if (!TryGetCellUnderMouse(rect, e.mousePosition, out int gx, out int gy)) return;
            if (!_puzzle.IsPlayable(gx, gy)) return;

            if (_puzzle.Sections.Count == 0 && e.button == 0)
                _currentSectionIndex = _puzzle.EnsureSectionCount(1) - 1;

            int newValue = e.button == 0 ? _currentSectionIndex : -1;
            if (_puzzle.GetSectionAt(gx, gy) != newValue)
            {
                if (newValue >= 0) _puzzle.EnsureSectionCount(newValue + 1);
                Undo.RegisterCompleteObjectUndo(_puzzle, "Paint Section");
                _puzzle.SetSectionAt(gx, gy, newValue);
                GUI.changed = true;
            }
            if (e.type == EventType.MouseDown) e.Use();
        }

        private int ComputeDefaultSectionTarget()
        {
            int playable = 0;
            for (int y = 0; y < _puzzle.Height; y++)
                for (int x = 0; x < _puzzle.Width; x++)
                    if (_puzzle.IsPlayable(x, y)) playable++;
            return Mathf.Clamp(Mathf.RoundToInt((float)playable / TargetCellsPerSection), MinSections, MaxSections);
        }

        private void AutoCluster()
        {
            Undo.RegisterCompleteObjectUndo(_puzzle, "Auto-Cluster Sections");
            var rng = new System.Random(_puzzle.name.GetHashCode());
            int target = ComputeDefaultSectionTarget();
            var ownership = SectionClusterer.AutoAssignSections(_puzzle.Width, _puzzle.Height, _puzzle.SourceArt.Colors, target, rng);

            int maxIndex = -1;
            for (int i = 0; i < ownership.Length; i++) maxIndex = Mathf.Max(maxIndex, ownership[i]);
            _puzzle.Sections.Clear();
            _puzzle.EnsureSectionCount(maxIndex + 1);
            for (int i = 0; i < ownership.Length; i++) _puzzle.SectionOf[i] = ownership[i];

            _currentSectionIndex = Mathf.Clamp(_currentSectionIndex, 0, Mathf.Max(0, _puzzle.Sections.Count - 1));
            GUI.changed = true;
        }

        /// <summary>The one-click fast path used both from Setup and from the in-editor toolbar:
        /// auto-cluster into sections, then auto-fill every section's regions.</summary>
        private void AutoGenerateEverything()
        {
            AutoCluster();
            for (int s = 0; s < _puzzle.Sections.Count; s++)
                AutoFillSection(s);
        }

        // ---------------------------------------------------------------- regions mode input

        private void HandleRegionDrawEvents(Rect rect)
        {
            Event e = Event.current;
            if (!rect.Contains(e.mousePosition) || e.alt) return;
            if (_editingSectionIndex >= _puzzle.Sections.Count) return;

            if (e.button == 1 && e.type == EventType.MouseDown)
            {
                if (TryGetCellUnderMouse(rect, e.mousePosition, out int rx, out int ry))
                {
                    var regions = _puzzle.Sections[_editingSectionIndex].Regions;
                    var hit = regions.FirstOrDefault(r => r.Rect.Contains(new Vector2Int(rx, ry)));
                    if (hit != null)
                    {
                        Undo.RegisterCompleteObjectUndo(_puzzle, "Delete Region");
                        regions.Remove(hit);
                        GUI.changed = true;
                    }
                }
                e.Use();
                return;
            }

            if (e.button != 0) return;

            if (e.type == EventType.MouseDown)
            {
                if (!TryGetCellUnderMouse(rect, e.mousePosition, out int gx, out int gy)) return;
                if (!IsCellFreeForRegion(gx, gy, _editingSectionIndex)) return;
                _dragging = true;
                _dragStart = new Vector2Int(gx, gy);
                _dragEnd = _dragStart;
                GUI.changed = true;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _dragging)
            {
                if (TryGetCellUnderMouse(rect, e.mousePosition, out int gx, out int gy))
                {
                    var candidate = new Vector2Int(gx, gy);
                    if (IsRectValidForNewRegion(MakeRect(_dragStart, candidate), _editingSectionIndex))
                        _dragEnd = candidate;
                }
                GUI.changed = true;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _dragging)
            {
                _dragging = false;
                var finalRect = MakeRect(_dragStart, _dragEnd);
                if (IsRectValidForNewRegion(finalRect, _editingSectionIndex))
                {
                    Undo.RegisterCompleteObjectUndo(_puzzle, "Draw Region");
                    _puzzle.Sections[_editingSectionIndex].Regions.Add(new PuzzleRegion
                    {
                        Rect = finalRect,
                        ClueCell = _dragStart,
                    });
                }
                GUI.changed = true;
                e.Use();
            }
        }

        private bool IsCellFreeForRegion(int x, int y, int sectionIndex)
        {
            if (_puzzle.GetSectionAt(x, y) != sectionIndex) return false;
            foreach (var region in _puzzle.Sections[sectionIndex].Regions)
                if (region.Rect.Contains(new Vector2Int(x, y))) return false;
            return true;
        }

        private bool IsRectValidForNewRegion(RectInt rect, int sectionIndex)
        {
            Color32? first = null;
            for (int y = rect.y; y < rect.y + rect.height; y++)
            {
                for (int x = rect.x; x < rect.x + rect.width; x++)
                {
                    if (!IsCellFreeForRegion(x, y, sectionIndex)) return false;
                    var c = _puzzle.SourceArt.GetColor(x, y);
                    if (first == null) first = c;
                    else if (!first.Value.Equals(c)) return false; // must stay monochrome
                }
            }
            return true;
        }

        private void AutoFillSection(int sectionIndex)
        {
            if (sectionIndex >= _puzzle.Sections.Count) return;

            int minX = _puzzle.Width, minY = _puzzle.Height, maxX = -1, maxY = -1;
            for (int y = 0; y < _puzzle.Height; y++)
                for (int x = 0; x < _puzzle.Width; x++)
                    if (_puzzle.GetSectionAt(x, y) == sectionIndex)
                    {
                        minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                    }
            if (maxX < minX) return; // section has no cells assigned yet - nothing to fill

            int bw = maxX - minX + 1, bh = maxY - minY + 1;
            var localColors = new Color32[bw * bh];
            var covered = new bool[_puzzle.Width * _puzzle.Height];
            foreach (var region in _puzzle.Sections[sectionIndex].Regions)
                for (int y = region.Rect.y; y < region.Rect.y + region.Rect.height; y++)
                    for (int x = region.Rect.x; x < region.Rect.x + region.Rect.width; x++)
                        covered[y * _puzzle.Width + x] = true;

            for (int y = 0; y < bh; y++)
            {
                for (int x = 0; x < bw; x++)
                {
                    int wx = minX + x, wy = minY + y;
                    bool remaining = _puzzle.GetSectionAt(wx, wy) == sectionIndex && !covered[wy * _puzzle.Width + wx];
                    localColors[y * bw + x] = remaining ? _puzzle.SourceArt.GetColor(wx, wy) : default;
                }
            }

            var newRegions = PixelShikakuGenerator.GenerateSection(bw, bh, localColors, MaxRegionArea, seed: _puzzle.name.GetHashCode() + sectionIndex * 97 + System.DateTime.Now.Millisecond);
            if (newRegions == null)
            {
                Debug.LogError($"PuzzleDesignerWindow: auto-fill failed its own coverage check for section {sectionIndex} - see Console.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(_puzzle, "Auto-Fill Regions");
            foreach (var r in newRegions)
            {
                _puzzle.Sections[sectionIndex].Regions.Add(new PuzzleRegion
                {
                    Rect = new RectInt(minX + r.Rect.x, minY + r.Rect.y, r.Rect.width, r.Rect.height),
                    ClueCell = new Vector2Int(minX + r.ClueCell.x, minY + r.ClueCell.y),
                });
            }
            GUI.changed = true;
        }

        // ---------------------------------------------------------------- verify & save

        private void DrawVerifyView()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Verify", GUILayout.Height(28), GUILayout.Width(100))) RunVerify();
            using (new EditorGUI.DisabledScope(!_verifyOk))
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Save && Register In Catalog", GUILayout.Height(28), GUILayout.Width(220))) SaveAndRegister();
                GUI.backgroundColor = Color.white;
            }
            GUILayout.EndHorizontal();

            var style = new GUIStyle(EditorStyles.label) { wordWrap = true };
            if (_verifyReport.Length > 0)
                style.normal.textColor = _verifyOk ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.7f, 0.2f, 0.2f);
            EditorGUILayout.LabelField(_verifyReport, style);
            GUILayout.EndVertical();

            float cellSize = Mathf.Min((position.width - 20f) / Mathf.Max(1, _puzzle.Width), 14f);
            Rect gridRect = GUILayoutUtility.GetRect(_puzzle.Width * cellSize, _puzzle.Height * cellSize);
            DrawFinalGrid(gridRect, cellSize);
        }

        private void DrawFinalGrid(Rect origin, float cellSize)
        {
            var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            foreach (var section in _puzzle.Sections)
            {
                foreach (var region in section.Regions)
                {
                    Rect r = new(
                        origin.x + region.Rect.x * cellSize,
                        origin.y + (_puzzle.Height - region.Rect.y - region.Rect.height) * cellSize,
                        region.Rect.width * cellSize,
                        region.Rect.height * cellSize);
                    EditorGUI.DrawRect(new Rect(r.x + 0.5f, r.y + 0.5f, r.width - 1f, r.height - 1f), _puzzle.SourceArt.GetColor(region.Rect.x, region.Rect.y));
                    if (cellSize >= 6f) GUI.Label(r, (region.Rect.width * region.Rect.height).ToString(), style);
                }
            }
        }

        /// <summary>
        /// Re-derives the exact hard invariants this project's own generator bugs have violated
        /// before: every region monochrome and non-overlapping, every Section's regions exactly
        /// cover the cells assigned to it, every playable picture cell assigned to exactly one
        /// Section. No separate asset is built here - PuzzleData already IS the runtime data.
        /// </summary>
        private void RunVerify()
        {
            _verifyOk = Validate(_puzzle, out _verifyReport);
        }

        /// <summary>
        /// The actual invariant checker, static so it can validate ANY PuzzleData - both the one
        /// currently open in the editor (RunVerify) and every candidate found on disk when
        /// rebuilding the catalog (SaveAndRegister), so a stale/incomplete puzzle sitting in
        /// Puzzles/ can never silently end up in the runtime catalog.
        /// </summary>
        private static bool Validate(PuzzleData puzzle, out string report)
        {
            var art = puzzle.SourceArt;
            var claims = new int[puzzle.Width * puzzle.Height];
            int badColorOrOverlap = 0, badSectionCoverage = 0, emptySections = 0, totalRegions = 0;
            var histogram = new Dictionary<int, int>();

            for (int s = 0; s < puzzle.Sections.Count; s++)
            {
                int assignedCount = 0;
                for (int y = 0; y < puzzle.Height; y++)
                {
                    for (int x = 0; x < puzzle.Width; x++)
                    {
                        if (puzzle.GetSectionAt(x, y) != s) continue;
                        assignedCount++;
                        claims[y * puzzle.Width + x]++;
                    }
                }
                if (assignedCount == 0) { emptySections++; continue; }

                var localCovered = new bool[puzzle.Width * puzzle.Height];
                int regionArea = 0;

                foreach (var region in puzzle.Sections[s].Regions)
                {
                    totalRegions++;
                    regionArea += region.Rect.width * region.Rect.height;
                    histogram.TryGetValue(region.Rect.width * region.Rect.height, out int c);
                    histogram[region.Rect.width * region.Rect.height] = c + 1;

                    Color32? first = null;
                    for (int y = region.Rect.y; y < region.Rect.y + region.Rect.height; y++)
                    {
                        for (int x = region.Rect.x; x < region.Rect.x + region.Rect.width; x++)
                        {
                            if (puzzle.GetSectionAt(x, y) != s) badColorOrOverlap++;
                            var col = art.GetColor(x, y);
                            if (col.a == 0) badColorOrOverlap++;
                            if (first == null) first = col; else if (!first.Value.Equals(col)) badColorOrOverlap++;

                            int idx = y * puzzle.Width + x;
                            if (localCovered[idx]) badColorOrOverlap++;
                            localCovered[idx] = true;
                        }
                    }
                }

                if (regionArea != assignedCount) badSectionCoverage++;
            }

            int unassignedPlayable = 0, badPictureCoverage = 0;
            for (int i = 0; i < claims.Length; i++)
            {
                int x = i % puzzle.Width, y = i / puzzle.Width;
                bool playable = puzzle.IsPlayable(x, y);
                if (playable && claims[i] == 0) unassignedPlayable++;
                else if (playable && claims[i] > 1) badPictureCoverage++;
                else if (!playable && claims[i] != 0) badPictureCoverage++;
            }

            bool ok = badColorOrOverlap == 0 && badSectionCoverage == 0 && badPictureCoverage == 0
                      && unassignedPlayable == 0 && emptySections == 0 && puzzle.Sections.Count > 0;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(ok ? "VERIFY: PASS" : "VERIFY: FAIL");
            sb.AppendLine($"Sections: {puzzle.Sections.Count}   Regions: {totalRegions}   Empty sections (no cells assigned): {emptySections}");
            sb.AppendLine($"Bad regions (cross-color / overlap / wrong section): {badColorOrOverlap}   Sections with mismatched region/assigned area: {badSectionCoverage}");
            sb.AppendLine($"Unassigned playable cells: {unassignedPlayable}   Picture cells claimed by >1 section: {badPictureCoverage}");
            sb.Append("Region size histogram: ");
            foreach (var kv in histogram.OrderBy(k => k.Key)) sb.Append($"[{kv.Key}:{kv.Value}] ");
            report = sb.ToString();
            return ok;
        }

        /// <summary>Saves the puzzle (already the final asset, no separate bake target) and
        /// rebuilds the runtime PuzzleCatalog by scanning every PuzzleData under Puzzles/, sorted
        /// by name - so solve order matches the usual "01_, 02_, ..." naming convention with no
        /// extra manual step. Each candidate is re-validated before being included, so a stale or
        /// still-incomplete puzzle sitting in the folder never silently ends up playable.</summary>
        private void SaveAndRegister()
        {
            if (!_verifyOk) return;

            EditorUtility.SetDirty(_puzzle);
            AssetDatabase.SaveAssets();

            if (!Directory.Exists(CatalogDir)) Directory.CreateDirectory(CatalogDir);
            var catalogPath = $"{CatalogDir}/{CatalogAssetName}.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<PuzzleCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = CreateInstance<PuzzleCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            var candidates = AssetDatabase.FindAssets("t:Shidaku.PuzzleData", new[] { PuzzleFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<PuzzleData>)
                .Where(p => p != null)
                .ToList();

            var validPuzzles = new List<PuzzleData>();
            int skipped = 0;
            foreach (var candidate in candidates)
            {
                if (Validate(candidate, out _)) validPuzzles.Add(candidate);
                else skipped++;
            }

            catalog.Puzzles.Clear();
            catalog.Puzzles.AddRange(validPuzzles);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            string skippedMsg = skipped > 0 ? $"\n\n{skipped} other puzzle(s) in the folder did NOT pass verification and were left out of the catalog." : "";
            EditorUtility.DisplayDialog("Saved", $"Puzzle saved:\n{AssetDatabase.GetAssetPath(_puzzle)}\n\nCatalog rebuilt with {catalog.Puzzles.Count} puzzle(s).{skippedMsg}", "OK");
        }
    }
}
#endif
